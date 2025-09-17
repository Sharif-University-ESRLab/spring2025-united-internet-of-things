using UnityEngine;
using System;

[System.Serializable]
public struct PollKey : IEquatable<PollKey>
{
    public GameObject gameObject;
    public int pollId;

    public PollKey(GameObject gameObject, int pollId)
    {
        this.gameObject = gameObject;
        this.pollId = pollId;
    }

    public bool Equals(PollKey other)
    {
        return gameObject == other.gameObject && pollId == other.pollId;
    }

    public override bool Equals(object obj)
    {
        return obj is PollKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(gameObject, pollId);
    }
}

public enum MessageType
{
    Poll,
    Response,
    Final
}

public class FinalMessageData 
{
    public float timeOfSendingPoll;
    public float timeOfReceivingResponse;
    public float timeOfSendingFinal;

    public FinalMessageData(float timeOfSendingPoll, float timeOfReceivingResponse, float timeOfSendingFinal)
    {
        this.timeOfSendingPoll = timeOfSendingPoll;
        this.timeOfReceivingResponse = timeOfReceivingResponse;
        this.timeOfSendingFinal = timeOfSendingFinal;
    }
}

public class MessageData
{
    public GameObject sender;
    public GameObject receiver;
    public MessageType messageType;
    public FinalMessageData finalMessageData;
    public int pollId;

    public MessageData(GameObject sender, GameObject receiver, MessageType messageType, int pollId, FinalMessageData finalMessageData = null)
    {
        this.sender = sender;
        this.messageType = messageType;
        this.receiver = receiver;
        this.pollId = pollId;
        this.finalMessageData = finalMessageData;
    }
}