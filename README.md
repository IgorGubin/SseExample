# SseExample

This example demonstrate the using server sent events (sse) technology when implementing the following algorithm:
- sending on server a some number of files for some processing,
- performing some processing of files on the server that changes their state,
- waiting on client by using sse when each file got the Completed state ,
- downloading files from the server in the Completed state and sending a command to the server to delete this file after successful download

> Server is implemented by using MVC technology.
> 
> Working with real files is simulated since the main goal of the example was to test the functionality of using sse within a specific client session,
for which the GET method on the server, participating in sse, accepts the sessionId parameter.

Additionaly, in example demonstrates the using of a conveyor of items that implementing pattern Producer\Consumer, and the using of the Channel class to map events from different places in the application code to queue ща concrete swssion used in controller SSE GET method.


