# YASLS.NET Software Development Kit for Modules

## Architecture ##

## Threading Model ##

### Module Thread Wrapper ###

Restart a module if it encounters an exception of finishes and need to re-connect.

## Interfaces ##

## Input/Output Module Patterns ##

### Threaded procedure ###
```csharp
      try
      {
        // Initialize your resources like sockets, file handles, DB connections, etc. here
        
        while (true)
        {
          // Start asynchronous operation
          Task<> asyncOperationResult = XXXAsync();
          try
          {
            // wait for the operation to complete, OR for the server to shut down
            asyncOperationResult.Wait(token);
          }
          catch (OperationCanceledException)
          {
            break;
          }

          // Use operation's results.
          asyncOperationResult.Result.Data;

          // Send Messages to the Queues
          if (bytesReceived > 0)
          {
            MessageDataItem message = CreateMessageDataItem(...);
            foreach (IServerMasterQueue queue in OutputQueues)
              queue.Enqueue(message);
          }
        }
      }
      finally
      {
        // free up resources
      }
```
### Module Initialization ###

```csharp
    public void Initialize(JObject configuration, CancellationToken cancellationToken, Dictionary<string, string> attributes, IEnumerable<IServerMasterQueue> queue)
    {
      token = cancellationToken;
      OutputQueues.AddRange(queue);
    }
```

## JSON Schema ##