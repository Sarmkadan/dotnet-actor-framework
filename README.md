// ... (rest of the file remains unchanged)

## ActorException

The `ActorException` is the base exception class for all actor-related errors in the DotNetActorFramework. It provides a foundation for creating specific actor exceptions and includes factory methods for creating formatted exception messages. All actor-specific exceptions such as `ActorNotFoundException`, `MailboxException`, `SupervisionException`, `ActorSystemException`, and `HttpActorCommunicationException` inherit from this base class.

### Usage Example

```csharp
public class OrderActor : Actor
{
    private readonly IOrderRepository _orderRepository;
    
    public OrderActor(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
    
    public async Task HandleMessage(ProcessOrderCommand command)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(command.OrderId);
            
            if (order == null)
            {
                throw ActorException.Create("Order with ID {0} not found", command.OrderId);
            }
            
            // Process the order
            order.Status = OrderStatus.Processed;
            await _orderRepository.UpdateAsync(order);
        }
        catch (ActorNotFoundException ex)
        {
            Log.Error(ex, "Actor not found: {ActorPath}", ex.ActorPath);
            throw;
        }
        catch (MailboxException ex) when (ex.ActorId != Guid.Empty)
        {
            Log.Error(ex, "Mailbox operation failed for actor {ActorId}: {Message}", ex.ActorId, ex.Message);
            throw;
        }
        catch (HttpActorCommunicationException ex)
        {
            Log.Error(ex, "HTTP communication failed for {RequestUrl}. Status: {StatusCode}", 
                     ex.RequestUrl, ex.StatusCode);
            throw;
        }
        catch (ActorException ex)
        {
            Log.Error(ex, "Actor operation failed: {Message}", ex.Message);
            throw ActorException.Create(ex, "Failed to process order command for order {0}", command.OrderId);
        }
    }
}
```

### Related Exceptions

- `ActorNotFoundException` - Thrown when an actor cannot be found in the system
- `MailboxException` - Thrown when a mailbox operation fails
- `SupervisionException` - Thrown when a supervision operation fails  
- `ActorSystemException` - Thrown when an actor system operation fails
- `HttpActorCommunicationException` - Thrown when HTTP communication with actors fails

## ValidationException

The `ValidationException` is a custom exception class used to handle validation-related errors in the actor framework. It provides a way to specify a custom error message and an inner exception for more detailed error handling.

### Usage Example
```csharp
public class MyActor : Actor
{
    public async Task HandleMessage(Message message)
    {
        try
        {
            // Attempt to validate the actor path
            var path = ActorPath.Parse("InvalidPath");
        }
        catch (ValidationException ex)
        {
            Log.Error(ex.Message);
            // Handle the validation error
        }
    }
}
```

## ConfigurationException

The `ConfigurationException` is a base exception class used to handle configuration-related errors in the actor framework. It provides a way to specify a custom error message and an inner exception for more detailed error handling. This exception has several derived classes, including `ActorSystemConfigurationException`, `MailboxConfigurationException`, and `PersistenceConfigurationException`, which can be used to handle specific configuration-related errors.

### Usage Example
```csharp
public class MyConfigurator
{
    public void ConfigureActorSystem()
    {
        try
        {
            // Attempt to configure the actor system
            var config = new ActorSystemOptions();
            // ...
        }
        catch (ConfigurationException ex)
        {
            Log.Error(ex.Message);
            // Handle the configuration error
        }
        catch (ActorSystemConfigurationException ex)
        {
            Log.Error(ex.Message);
            // Handle the actor system configuration error
        }
    }
}
```

// ... (rest of the file remains unchanged)

## ActorException

The `ActorException` is the base exception class for all actor-related errors in the DotNetActorFramework. It provides a foundation for creating specific actor exceptions and includes factory methods for creating formatted exception messages. All actor-specific exceptions such as `ActorNotFoundException`, `MailboxException`, `SupervisionException`, `ActorSystemException`, and `HttpActorCommunicationException` inherit from this base class.

### Usage Example

```csharp
public class OrderActor : Actor
{
    private readonly IOrderRepository _orderRepository;
    
    public OrderActor(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
    
    public async Task HandleMessage(ProcessOrderCommand command)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(command.OrderId);
            
            if (order == null)
            {
                throw ActorException.Create("Order with ID {0} not found", command.OrderId);
            }
            
            // Process the order
            order.Status = OrderStatus.Processed;
            await _orderRepository.UpdateAsync(order);
        }
        catch (ActorNotFoundException ex)
        {
            Log.Error(ex, "Actor not found: {ActorPath}", ex.ActorPath);
            throw;
        }
        catch (MailboxException ex) when (ex.ActorId != Guid.Empty)
        {
            Log.Error(ex, "Mailbox operation failed for actor {ActorId}: {Message}", ex.ActorId, ex.Message);
            throw;
        }
        catch (HttpActorCommunicationException ex)
        {
            Log.Error(ex, "HTTP communication failed for {RequestUrl}. Status: {StatusCode}", 
                     ex.RequestUrl, ex.StatusCode);
            throw;
        }
        catch (ActorException ex)
        {
            Log.Error(ex, "Actor operation failed: {Message}", ex.Message);
            throw ActorException.Create(ex, "Failed to process order command for order {0}", command.OrderId);
        }
    }
}
```

### Related Exceptions

- `ActorNotFoundException` - Thrown when an actor cannot be found in the system
- `MailboxException` - Thrown when a mailbox operation fails
- `SupervisionException` - Thrown when a supervision operation fails  
- `ActorSystemException` - Thrown when an actor system operation fails
- `HttpActorCommunicationException` - Thrown when HTTP communication with actors fails
