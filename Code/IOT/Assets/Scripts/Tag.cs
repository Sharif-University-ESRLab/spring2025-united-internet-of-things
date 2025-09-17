using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class Tag : MonoBehaviour
{
    private Dictionary<PollKey, float> timeOfSendingPolls = new Dictionary<PollKey, float>();
    private int currentPollId = 0;
    private List<GameObject> anchors;
    private Dictionary<int, int> pollResponseCounts = new Dictionary<int, int>();
    
    [SerializeField] private float twrInterval = 1.0f; // Time between TWR procedures in seconds

    void Start()
    {
        anchors = new List<GameObject>(GameObject.FindGameObjectsWithTag("Anchor"));
        StartTWRProcedure();
    }

    private void StartTWRProcedure()
    {
        currentPollId++;
        pollResponseCounts[currentPollId] = 0;
        
        Debug.Log($"{name} starting TWR procedure with poll ID {currentPollId}");
        
        foreach (GameObject anchor in anchors)
        {
            PollKey pollKey = new PollKey(anchor, currentPollId);
            timeOfSendingPolls[pollKey] = MessageBus.Instance.GetSimulatedTime(gameObject);
            MessageBus.Instance.SendMessageDelayed(new MessageData(gameObject, anchor, MessageType.Poll, currentPollId));
        }
    }

    public void OnMessageReceived(MessageData message) 
    {
        Debug.Log($"{name} received message from {message.sender.name} with poll ID {message.pollId} at {MessageBus.Instance.GetSimulatedTime(gameObject):F6}");
        Debug.Assert(message.messageType == MessageType.Response, "Unknown message type");
        
        PollKey pollKey = new PollKey(message.sender, message.pollId);
        float timeOfSendingPoll = timeOfSendingPolls[pollKey];
        float timeOfReceivingResponse = MessageBus.Instance.GetSimulatedTime(gameObject);
        float timeOfSendingFinal = MessageBus.Instance.GetSimulatedTime(gameObject);
        FinalMessageData finalMessageData = new FinalMessageData(timeOfSendingPoll, timeOfReceivingResponse, timeOfSendingFinal);
        MessageBus.Instance.SendMessageDelayed(new MessageData(gameObject, message.sender, MessageType.Final, message.pollId, finalMessageData));
        
        // Track responses for this poll ID
        if (pollResponseCounts.ContainsKey(message.pollId))
        {
            pollResponseCounts[message.pollId]++;
            
            // Check if we've received all responses for this poll
            if (pollResponseCounts[message.pollId] >= anchors.Count)
            {
                Debug.Log($"{name} completed TWR procedure for poll ID {message.pollId}");
                
                // Clean up completed poll data
                CleanupPollData(message.pollId);
                
                // Schedule next TWR procedure
                StartCoroutine(ScheduleNextTWR());
            }
        }
    }
    
    private void CleanupPollData(int pollId)
    {
        // Remove poll data for completed poll
        var keysToRemove = new List<PollKey>();
        foreach (var key in timeOfSendingPolls.Keys)
        {
            if (key.pollId == pollId)
            {
                keysToRemove.Add(key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            timeOfSendingPolls.Remove(key);
        }
        
        pollResponseCounts.Remove(pollId);
    }
    
    private IEnumerator ScheduleNextTWR()
    {
        yield return new WaitForSeconds(twrInterval);
        StartTWRProcedure();
    }
}
