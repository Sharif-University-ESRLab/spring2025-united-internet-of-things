using UnityEngine;

public struct DistanceReport
{
    public GameObject tag;
    public Vector3 anchorPosition;
    public float distance;
    public int pollId;

    public DistanceReport(GameObject tag, Vector3 anchorPosition, float distance, int pollId)
    {
        this.tag = tag;
        this.anchorPosition = anchorPosition;
        this.distance = distance;
        this.pollId = pollId;
    }
}