// This Server class is no longer used in this project
// The server functionality has been moved to a separate TCP server application
/*
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class Server : MonoBehaviour
{
    public static Server Instance;

    // We collect distance reports per tag and poll ID
    private Dictionary<GameObject, Dictionary<int, List<DistanceReport>>> tagPollReports = new Dictionary<GameObject, Dictionary<int, List<DistanceReport>>>();

    private List<GameObject> allAnchors = new List<GameObject>();

    void Start()
    {
        allAnchors = GameObject.FindGameObjectsWithTag("Anchor").ToList();
    }

    void Awake()
    {
        Instance = this;
    }

    public void ReceiveDistanceReport(DistanceReport report)
    {
        if (!tagPollReports.ContainsKey(report.tag))
        {
            tagPollReports[report.tag] = new Dictionary<int, List<DistanceReport>>();
        }

        if (!tagPollReports[report.tag].ContainsKey(report.pollId))
        {
            tagPollReports[report.tag][report.pollId] = new List<DistanceReport>();
        }

        var reports = tagPollReports[report.tag][report.pollId];

        // Replace any existing report from this anchor for this poll
        int existingIndex = reports.FindIndex(r => r.anchorPosition == report.anchorPosition);
        if (existingIndex != -1)
        {
            reports[existingIndex] = report;
        }
        else
        {
            reports.Add(report);
        }

        // ✅ Wait until all anchors have reported for this specific poll
        if (reports.Count == allAnchors.Count)
        {
            Vector3 estimatedPosition = Trilaterate(reports);
            Debug.Log($"[Server] Tag: {report.tag.name}, Poll ID: {report.pollId}, Estimated Position: {estimatedPosition}, Actual: {report.tag.transform.position}, Error: {Vector3.Distance(estimatedPosition, report.tag.transform.position):F6} m");

            // Write estimated position to file associated with tag name
            WritePositionToFile(report.tag.name, estimatedPosition, report.tag.transform.position, report.pollId);

            // Clear this specific poll's data
            tagPollReports[report.tag].Remove(report.pollId);
        }
    }

    // Simple trilateration with 3 anchors
    private Vector3 Trilaterate(List<DistanceReport> reports)
    {
        int N = reports.Count;
        if (N < 3)
        {
            Debug.LogError("At least 3 distance reports are required for trilateration.");
            return Vector3.zero;
        }

        // Use the first anchor as reference
        Vector3 p1 = reports[0].anchorPosition;
        float r1 = reports[0].distance;

        // Build the linear system A*x = b
        // Each row i in A corresponds to anchor i+1:
        // A[i] = 2*(p_i+1 - p1)
        // b[i] = r1^2 - r_i+1^2 + ||p_i+1||^2 - ||p1||^2
        Matrix4x4 AtA = new Matrix4x4();
        Vector4 Atb = Vector4.zero;

        for (int i = 1; i < N; i++)
        {
            Vector3 pi = reports[i].anchorPosition;
            float ri = reports[i].distance;

            Vector3 Arow = 2 * (pi - p1);
            float bi = r1 * r1 - ri * ri + pi.sqrMagnitude - p1.sqrMagnitude;

            // Accumulate AtA = A^T A and Atb = A^T b
            AtA.m00 += Arow.x * Arow.x;
            AtA.m01 += Arow.x * Arow.y;
            AtA.m02 += Arow.x * Arow.z;

            AtA.m10 += Arow.y * Arow.x;
            AtA.m11 += Arow.y * Arow.y;
            AtA.m12 += Arow.y * Arow.z;

            AtA.m20 += Arow.z * Arow.x;
            AtA.m21 += Arow.z * Arow.y;
            AtA.m22 += Arow.z * Arow.z;

            Atb.x += Arow.x * bi;
            Atb.y += Arow.y * bi;
            Atb.z += Arow.z * bi;
        }

        // Complete symmetric matrix
        AtA.m03 = AtA.m30 = 0f;
        AtA.m13 = AtA.m31 = 0f;
        AtA.m23 = AtA.m32 = 0f;
        AtA.m33 = 1f;

        AtA.m33 = 1f;

        // Solve AtA * x = Atb
        Vector3 result = AtA.inverse.MultiplyVector(new Vector3(Atb.x, Atb.y, Atb.z));

        return result;
    }

    private void WritePositionToFile(string tagName, Vector3 estimatedPosition, Vector3 actualPosition, int pollId)
    {
        string fileName = $"{tagName}.txt";
        string resultsDir = Path.Combine(Application.dataPath, "..", "Results");
        string filePath = Path.Combine(resultsDir, fileName);
        
        // Create Results directory if it doesn't exist
        if (!Directory.Exists(resultsDir))
        {
            Directory.CreateDirectory(resultsDir);
            Debug.Log($"[Server] Created Results directory: {resultsDir}");
        }
        
        string positionData = $"{estimatedPosition.x:F6},{estimatedPosition.y:F6},{estimatedPosition.z:F6}";
        
        try
        {
            File.WriteAllText(filePath, positionData);
            Debug.Log($"[Server] Position data for poll {pollId} written to: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Server] Failed to write position data: {e.Message}");
        }
    }
}
*/