# SseExample
Simplified example from branch main to demonstrate a test case with a problem when using sse technology

# Question for the developer of server sent events 
[dotnet/runtime/issues/114923](https://github.com/dotnet/runtime/issues/114923)

# Test case of the problem
1) publish client and server applications
2) deploy the client's sse console application, in two folders for example 
   - C:\MS\SSE\client1
   - C:\MS\SSE\client2
     
   >In config.json file in each of clients installfolder set different value to parameter curSessionId from variants: "4380109fac434ffb9ecf195f321c6226", "6dc8964913e84131a1100db093a05866".
4) run the server application in visual studio for step by step debugging
5) start two cmd.exe processes and in these processes run the client's console applications C:\MS\SSE\client1\Client.exe and C:\MS\SSE\client2\Client.exe one after another
6) sooner or later both clients will stop at the waiting step because the following will happen
- actual behavior: At some point only one of the clients receives messages for itself and for the other client, while the other client does not receive the messages it expects and remains in the waiting state forever. And vice versa. This leads both clients to the blocking state.
- expected behavior: each client's console application receives all server event messages, selects those from them by eventId that relate to its session, processes them and waits for the next events.

![Figure 1](sse_problem.png)

# Notes to testing
1) The target of testing is log files of each of cliens.
2) You should pay attention in log file on strings where Sessionid is not equal EventId like this:
```
>>> Sessionid: "4380109fac434ffb9ecf195f321c6226"  - got data:
EventId: "6dc8964913e84131a1100db093a05866";
EventType: "StateInfo";
EventData: {FileId: "4e078833bfe24d32a5520433f810988d"; State: Сompleted}; 
``` 
For such string should be exists a paired string in the log file of another client where Sessionid is equal EventId like this:

```
>>> Sessionid: "6dc8964913e84131a1100db093a05866"  - got data:
EventId: "6dc8964913e84131a1100db093a05866";
EventType: "StateInfo";
EventData: {FileId: "4e078833bfe24d32a5520433f810988d"; State: Сompleted}; 
``` 
If paired string is not exists it marks the problem.
