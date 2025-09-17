using UnityEngine;
using System.IO;
using System.Globalization;

public class CoordinateReader : MonoBehaviour
{
    public GameObject dataPointPrefab;
    public string filePath = "coordinates.txt";
    public float scaleFactor = 1f;
    public float updateInterval = 0.01f; // Time in seconds between update

    private float lastUpdateTime = 0f;
    private System.DateTime lastFileWriteTime;

    void Start()
    {
        ReadCoordinatesFromFile();
        if (File.Exists(filePath))
        {
            lastFileWriteTime = File.GetLastWriteTime(filePath);
        }
    }

    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            lastUpdateTime = Time.time;
            CheckForFileUpdates();
        }
    }

    void CheckForFileUpdates()
    {
        if (File.Exists(filePath))
        {
            System.DateTime currentFileWriteTime = File.GetLastWriteTime(filePath);
            if (currentFileWriteTime > lastFileWriteTime)
            {
                Debug.Log("Coordinate file has been updated. Reloading...");
                lastFileWriteTime = currentFileWriteTime;
                ReadCoordinatesFromFile();
            }
        }
        else
        {
            Debug.LogError($"File not found: {filePath}");
        }
    }

    void ReadCoordinatesFromFile()
    {
        foreach (Transform child in transform) //delete
        {
            Destroy(child.gameObject);
        }

        try
        {
            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    string[] coordinates = line.Split(new char[] { ',', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

                    if (coordinates.Length == 3)
                    {
                        if (float.TryParse(coordinates[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                            float.TryParse(coordinates[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                            float.TryParse(coordinates[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                        {
                            GameObject dataPoint = Instantiate(dataPointPrefab, transform); // Instantiate as a child of this GameObject
                            dataPoint.transform.localPosition = new Vector3(x * scaleFactor, y * scaleFactor, z * scaleFactor);
                            
                            Debug.Log($"[CoordinateReader] Parsed coordinates from {Path.GetFileName(filePath)}: x={x:F6}, y={y:F6}, z={z:F6}");
                        }
                        else
                        {
                            Debug.LogError($"Error parsing coordinates in line: {line}.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Skipping invalid line in file: {line}.");
                    }
                }
            }
            else
            {
                Debug.LogError($"File not found: {filePath}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"An error occurred while reading the file: {ex.Message}");
        }
    }
}