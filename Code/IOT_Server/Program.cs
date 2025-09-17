using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

class Program
{
    // Configuration
    private const int Port = 5000;
    private const float SpeedOfLight = 299_792_458f;
    
    // We collect distance reports per tag and poll ID
    private static Dictionary<string, Dictionary<int, List<DistanceReport>>> tagPollReports = 
        new Dictionary<string, Dictionary<int, List<DistanceReport>>>();
        
    // Keep track of anchor counts per tag
    private static Dictionary<string, int> tagExpectedAnchors = new Dictionary<string, int>();

    static void Main(string[] args)
    {
        Console.WriteLine("IOT Server Starting...");
        string resultsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Results");
        
        // Create Results directory if it doesn't exist
        if (!Directory.Exists(resultsDir))
        {
            Directory.CreateDirectory(resultsDir);
            Console.WriteLine($"Created Results directory: {resultsDir}");
        }
        
        StartServer();
    }

    static void StartServer()
    {
        // Create TCP/IP socket
        TcpListener server = new TcpListener(IPAddress.Any, Port);
        
        try
        {
            // Start listening for client requests
            server.Start();
            Console.WriteLine($"Server started on port {Port}");

            // Enter the listening loop
            while (true)
            {
                Console.WriteLine("Waiting for a connection...");
                
                // Perform a blocking call to accept requests
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connected to a client!");

                // Create a thread to handle communication with this client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start(client);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
        finally
        {
            // Stop listening for new clients
            server.Stop();
        }
    }

    static void HandleClient(object clientObj)
    {
        TcpClient client = (TcpClient)clientObj;
        NetworkStream stream = client.GetStream();
        
        byte[] buffer = new byte[1024];
        int bytesRead;
        
        try
        {
            // Loop to receive all data sent by the client
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received: {data}");
                
                // Parse the received data
                DistanceReport report = ParseReport(data);
                
                // Process the report
                ProcessReport(report);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error handling client: {e.Message}");
        }
        finally
        {
            // Close the connection
            client.Close();
        }
    }

    static DistanceReport ParseReport(string data)
    {
        // Format: "tagName,pollId,anchorName,anchorPosX,anchorPosY,anchorPosZ,distance"
        string[] parts = data.Split(',');
        if (parts.Length != 7)
        {
            throw new FormatException("Invalid data format");
        }
        
        string tagName = parts[0];
        int pollId = int.Parse(parts[1]);
        string anchorName = parts[2];
        float anchorPosX = float.Parse(parts[3]);
        float anchorPosY = float.Parse(parts[4]);
        float anchorPosZ = float.Parse(parts[5]);
        float distance = float.Parse(parts[6]);
        
        return new DistanceReport(
            tagName,
            new Vector3(anchorPosX, anchorPosY, anchorPosZ),
            distance,
            pollId,
            anchorName
        );
    }

    static void ProcessReport(DistanceReport report)
    {
        // Create dictionaries if they don't exist
        if (!tagPollReports.ContainsKey(report.tagName))
        {
            tagPollReports[report.tagName] = new Dictionary<int, List<DistanceReport>>();
        }

        if (!tagPollReports[report.tagName].ContainsKey(report.pollId))
        {
            tagPollReports[report.tagName][report.pollId] = new List<DistanceReport>();
        }
        
        // Get the reports list
        var reports = tagPollReports[report.tagName][report.pollId];
        
        // Replace existing report from this anchor or add new one
        int existingIndex = reports.FindIndex(r => r.anchorName == report.anchorName);
        if (existingIndex != -1)
        {
            reports[existingIndex] = report;
        }
        else
        {
            reports.Add(report);
        }
        
        // Update expected anchor count if needed
        if (!tagExpectedAnchors.ContainsKey(report.tagName) || reports.Count > tagExpectedAnchors[report.tagName])
        {
            tagExpectedAnchors[report.tagName] = reports.Count;
        }
        
        // Check if we have received all reports for this tag and poll ID
        if (tagExpectedAnchors.ContainsKey(report.tagName) && 
            reports.Count >= tagExpectedAnchors[report.tagName])
        {
            Vector3 estimatedPosition = Trilaterate(reports);
            Console.WriteLine($"Tag: {report.tagName}, Poll ID: {report.pollId}, " +
                $"Estimated Position: ({estimatedPosition.X:F6}, {estimatedPosition.Y:F6}, {estimatedPosition.Z:F6})");
            
            // Write position to file
            WritePositionToFile(report.tagName, estimatedPosition, report.pollId);
            
            // Clear this poll's data
            tagPollReports[report.tagName].Remove(report.pollId);
        }
    }
    
    static Vector3 Trilaterate(List<DistanceReport> reports)
    {
        int N = reports.Count;
        if (N < 3)
        {
            Console.WriteLine("At least 3 distance reports are required for trilateration.");
            return Vector3.Zero;
        }

        // Use the first anchor as reference
        Vector3 p1 = reports[0].anchorPosition;
        float r1 = reports[0].distance;

        // Build the linear system A*x = b
        // Each row i in A corresponds to anchor i+1:
        // A[i] = 2*(p_i+1 - p1)
        // b[i] = r1^2 - r_i+1^2 + ||p_i+1||^2 - ||p1||^2
        float[,] AtA = new float[3, 3];
        float[] Atb = new float[3];

        for (int i = 1; i < N; i++)
        {
            Vector3 pi = reports[i].anchorPosition;
            float ri = reports[i].distance;

            Vector3 Arow = 2 * (pi - p1);
            float bi = r1 * r1 - ri * ri + pi.LengthSquared() - p1.LengthSquared();

            // Accumulate AtA = A^T A and Atb = A^T b
            AtA[0, 0] += Arow.X * Arow.X;
            AtA[0, 1] += Arow.X * Arow.Y;
            AtA[0, 2] += Arow.X * Arow.Z;

            AtA[1, 0] += Arow.Y * Arow.X;
            AtA[1, 1] += Arow.Y * Arow.Y;
            AtA[1, 2] += Arow.Y * Arow.Z;

            AtA[2, 0] += Arow.Z * Arow.X;
            AtA[2, 1] += Arow.Z * Arow.Y;
            AtA[2, 2] += Arow.Z * Arow.Z;

            Atb[0] += Arow.X * bi;
            Atb[1] += Arow.Y * bi;
            Atb[2] += Arow.Z * bi;
        }

        // Solve AtA * x = Atb using Cramer's rule for simplicity
        float det = AtA[0, 0] * (AtA[1, 1] * AtA[2, 2] - AtA[1, 2] * AtA[2, 1]) -
                   AtA[0, 1] * (AtA[1, 0] * AtA[2, 2] - AtA[1, 2] * AtA[2, 0]) +
                   AtA[0, 2] * (AtA[1, 0] * AtA[2, 1] - AtA[1, 1] * AtA[2, 0]);

        if (Math.Abs(det) < 1e-10)
        {
            Console.WriteLine("Singular matrix, cannot solve system");
            return Vector3.Zero;
        }

        // Replace columns of AtA with Atb to find determinants for each unknown
        float detX = Atb[0] * (AtA[1, 1] * AtA[2, 2] - AtA[1, 2] * AtA[2, 1]) -
                    AtA[0, 1] * (Atb[1] * AtA[2, 2] - AtA[1, 2] * Atb[2]) +
                    AtA[0, 2] * (Atb[1] * AtA[2, 1] - AtA[1, 1] * Atb[2]);

        float detY = AtA[0, 0] * (Atb[1] * AtA[2, 2] - AtA[1, 2] * Atb[2]) -
                    Atb[0] * (AtA[1, 0] * AtA[2, 2] - AtA[1, 2] * AtA[2, 0]) +
                    AtA[0, 2] * (AtA[1, 0] * Atb[2] - Atb[1] * AtA[2, 0]);

        float detZ = AtA[0, 0] * (AtA[1, 1] * Atb[2] - Atb[1] * AtA[2, 1]) -
                    AtA[0, 1] * (AtA[1, 0] * Atb[2] - Atb[1] * AtA[2, 0]) +
                    Atb[0] * (AtA[1, 0] * AtA[2, 1] - AtA[1, 1] * AtA[2, 0]);

        // Compute the solution
        float x = detX / det;
        float y = detY / det;
        float z = detZ / det;

        return new Vector3(x, y, z);
    }
    
    static void WritePositionToFile(string tagName, Vector3 estimatedPosition, int pollId)
    {
        string fileName = $"{tagName}.txt";
        string resultsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Results");
        string filePath = Path.Combine(resultsDir, fileName);
        
        string positionData = $"{estimatedPosition.X:F6},{estimatedPosition.Y:F6},{estimatedPosition.Z:F6}";
        
        try
        {
            File.WriteAllText(filePath, positionData);
            Console.WriteLine($"Position data for poll {pollId} written to: {filePath}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to write position data: {e.Message}");
        }
    }
}

// Data structure to represent distance reports sent from anchors
public struct DistanceReport
{
    public string tagName;
    public Vector3 anchorPosition;
    public float distance;
    public int pollId;
    public string anchorName;

    public DistanceReport(string tagName, Vector3 anchorPosition, float distance, int pollId, string anchorName)
    {
        this.tagName = tagName;
        this.anchorPosition = anchorPosition;
        this.distance = distance;
        this.pollId = pollId;
        this.anchorName = anchorName;
    }
}

