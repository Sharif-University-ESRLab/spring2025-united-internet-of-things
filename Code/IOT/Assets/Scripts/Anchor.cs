using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System;

public class Anchor : MonoBehaviour
{
    const float SPEED_OF_LIGHT = 299_792_458f;
    
    // TCP Server configuration
    private const string ServerIP = "127.0.0.1";
    private const int ServerPort = 5000;

    private Dictionary<PollKey, float> timeOfReceivingPolls = new Dictionary<PollKey, float>();
    private Dictionary<PollKey, float> timeOfSendingResponses = new Dictionary<PollKey, float>();

    public void OnMessageReceived(MessageData message)
    {
        Debug.Log($"{name} received message from {message.sender.name} with poll ID {message.pollId} at {MessageBus.Instance.GetSimulatedTime(gameObject):F6}");
        
        PollKey pollKey = new PollKey(message.sender, message.pollId);
        
        if (message.messageType == MessageType.Poll) 
        {
            timeOfReceivingPolls[pollKey] = MessageBus.Instance.GetSimulatedTime(gameObject);
            timeOfSendingResponses[pollKey] = MessageBus.Instance.GetSimulatedTime(gameObject);
            MessageBus.Instance.SendMessageDelayed(new MessageData(gameObject, message.sender, MessageType.Response, message.pollId));
        } else if (message.messageType == MessageType.Final)
        {
            float timeOfSendingResponse = timeOfSendingResponses[pollKey];
            float timeOfReceivingPoll = timeOfReceivingPolls[pollKey];
            float timeOfReceivingFinal = MessageBus.Instance.GetSimulatedTime(gameObject);
            float timeOfSendingPoll = message.finalMessageData.timeOfSendingPoll;
            float timeOfReceivingResponse = message.finalMessageData.timeOfReceivingResponse;
            float timeOfSendingFinal = message.finalMessageData.timeOfSendingFinal;
            float timeOfFlight = ((timeOfReceivingResponse - timeOfSendingPoll) - (timeOfSendingResponse - timeOfReceivingPoll) + (timeOfReceivingFinal - timeOfSendingResponse) - (timeOfSendingFinal - timeOfReceivingResponse)) / 4;
            float estimatedDistance = timeOfFlight * SPEED_OF_LIGHT;
            float actualDistance = Vector3.Distance(transform.position, message.sender.transform.position);
            float error = Mathf.Abs(estimatedDistance - actualDistance);
            Debug.Log($"Tag ID: {message.sender.name} Poll ID: {message.pollId} - Estimated Distance: {estimatedDistance:F6} m, Actual Distance: {actualDistance:F6} m, Error: {error:F6} m");
            
            // Send distance report to TCP server instead of local Server instance
            SendDistanceReportToServer(message.sender.name, message.pollId, estimatedDistance);
            
            // Clean up the dictionaries for this poll
            timeOfReceivingPolls.Remove(pollKey);
            timeOfSendingResponses.Remove(pollKey);
        } else 
        {
            Debug.Assert(false, "Unknown message type");
        }
    }
    
    private void SendDistanceReportToServer(string tagName, int pollId, float estimatedDistance)
    {
        try
        {
            using (TcpClient client = new TcpClient(ServerIP, ServerPort))
            {
                NetworkStream stream = client.GetStream();
                
                // Format: "tagName,pollId,anchorName,anchorPosX,anchorPosY,anchorPosZ,distance"
                string data = $"{tagName},{pollId},{name},{transform.position.x:F6},{transform.position.y:F6},{transform.position.z:F6},{estimatedDistance:F6}";
                
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                stream.Write(dataBytes, 0, dataBytes.Length);
                
                Debug.Log($"[Anchor {name}] Sent distance report to server: {data}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Anchor {name}] Failed to send distance report to server: {e.Message}");
        }
    }
}