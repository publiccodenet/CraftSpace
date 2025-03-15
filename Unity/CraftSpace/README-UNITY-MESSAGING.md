# Unity-JavaScript Messaging System

## Messaging Architecture

CraftSpace uses a bidirectional messaging system between JavaScript and Unity:

```
┌─────────────────┐         ┌─────────────────┐
│                 │  JSON   │                 │
│   JavaScript    │ ─────► │  Unity WebGL    │
│   Application   │ ◄───── │  Application    │
│                 │ Messages│                 │
└─────────────────┘         └─────────────────┘
```

This architecture allows:
- JavaScript to control Unity visualization
- Unity to report back user interactions
- Separation of concerns between platforms
- Type-safe communication via shared schemas

## Message Structure

All messages follow a consistent structure:

```json
{
  "type": "MESSAGE_TYPE",
  "data": {
    // Message-specific payload
  },
  "id": "optional-correlation-id",
  "timestamp": "2023-04-01T12:34:56.789Z"
}
```

Where:
- `type`: Identifies the message purpose and handler
- `data`: Contains the payload specific to the message type
- `id`: Optional correlation ID for request/response patterns
- `timestamp`: When the message was created

## Type Safety

Messages are validated against JSON schemas:

1. **Schema Definition**: Zod schemas define message formats
2. **Schema Export**: Schemas exported to JSON Schema format
3. **C# Class Generation**: C# classes generated from schemas
4. **Runtime Validation**: Messages validated on both ends

## Performance Optimizations

To maintain performance, especially in WebGL:

1. **Message Batching**: Group related updates into single messages
2. **Throttling**: Limit message frequency for high-volume updates
3. **Binary Data**: Use binary formats for large datasets
4. **Prioritization**: Critical messages processed first

## Common Message Types

| Direction        | Type                 | Purpose                               |
|------------------|----------------------|---------------------------------------|
| JS → Unity       | INITIALIZE           | Set up the visualization              |
| JS → Unity       | LOAD_COLLECTION      | Load a collection into the view       |
| JS → Unity       | UPDATE_ITEM          | Update a specific item                |
| JS → Unity       | CAMERA_CONTROL       | Control the camera position/target    |
| Unity → JS       | READY                | Unity initialization complete         |
| Unity → JS       | ITEM_SELECTED        | User selected an item                 |
| Unity → JS       | VIEW_CHANGED         | Camera/view position changed          |
| Unity → JS       | ERROR                | Error occurred in Unity               |

## Implementation Notes

- **Message Queue**: Messages processed in order
- **Error Handling**: Protocol for handling malformed messages
- **Reconnection**: Handling WebGL context losses
- **Logging**: Optional verbose logging for debugging

## Integration with UnityJS

This messaging system integrates with the existing UnityJS framework, leveraging its proven messaging infrastructure while adapting it to CraftSpace's specific needs.

## Best Practices

1. **Keep Messages Small**: Minimize payload size
2. **Use Message Schemas**: Ensure type safety
3. **Handle Errors Gracefully**: Recover from communication errors
4. **Batch Updates**: Group related changes
5. **Consider Timing**: Be aware of frame timing in Unity 

## Admiration Marking Concept

The admiration marking concept is interesting - it's essentially a social tagging/rating system inspired by animal territory marking behavior, but repurposed as a positive sharing mechanism. Users can "mark" items they admire with their unique "flavor" of appreciation, which gets aggregated with other users' marks to create a kind of collective evaluation and recommendation system.

This feeds into recommendation systems and collective curation. 