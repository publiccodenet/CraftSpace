# CraftSpace Unity Coding Guidelines

## Logging Practices

### Single-Line Log Pattern

All logging should follow a "one line per log" approach to facilitate easy commenting out during development:

```csharp
// Correct - single line for easy commenting
Logger.Info("ClassName", "MethodName", "Message here", new Dictionary<string, object> { { "param1", value1 }, { "param2", value2 }, { "param3", value3 } });

// Avoid - multiline logging is harder to comment out
Logger.Info("ClassName", "MethodName", "Message here", new Dictionary<string, object> { 
    { "param1", value1 },
    { "param2", value2 },
    { "param3", value3 }
});
```

### Development Phases for Logging

1. **Initial Development**: Log everything with detailed parameters
   - Per-item logs for all operations
   - Detailed parameter values
   - Loading/initialization details
   - All potential error points

2. **Testing Phase**: Comment out routine logs
   - Keep all error logs active
   - Keep important milestone logs
   - Comment out routine success logs with `//` 

3. **Production**: Only essential logs remain active
   - Only errors, warnings, and critical milestones
   - Performance-related logs stay active but verbose flag-gated

### Log Levels & Emoji Usage

Specific emoji indicators provide visual cues about log types:

- **✅ SUCCESS**: Operations completed successfully  
- **ℹ️ INFO**: Routine information, not errors
- **⚠️ WARNING**: Potential issues that aren't blocking
- **❌ ERROR**: Failed operations requiring attention
- **🚀 START**: Beginning of an operation  
- **🏁 COMPLETE**: End of an operation
- **📊 STATS**: Performance or metrics information
- **📦 LOAD**: Content or resource loading
- **📚 COLLECTION**: Collection-related operations
- **🧩 ITEM**: Item-related operations

### Performance Considerations

- **Conditional Logging**: Use the verbose flag to gate detailed logs
- **Comment Out Strategy**: Comment out logs, don't delete them
- **Parameter Construction**: For very performance-critical paths, wrap logs in conditions:

```csharp
if (verbose) {
    var logParams = new Dictionary<string, object> { {"key", value}, {"computedValue", ExpensiveComputation()} };
    Logger.Info("Class", "Method", "Message", logParams);
}
```

+ ### GameObject Context References
+ 
+ Unity's console can select GameObjects when clicking on log entries if they're passed as context:
+ 
+ ```csharp
+ // Pass 'this.gameObject' (or any GameObject) as the last parameter for clickable logs
+ Logger.Info("ItemView", "UpdateView", "Updating view", params, this.gameObject);
+ ```
+ 
+ Benefits of context objects:
+ 
+ - **Quick Navigation**: Click log to select the GameObject in hierarchy
+ - **Visual Debugging**: See object transform, components, and state
+ - **Easier Tracing**: Connect logs to specific scene objects
+ 
+ Best practices:
+ 
+ - Include context for MonoBehaviour component logs
+ - Don't include context for static utility methods
+ - For pooled objects, include context when available 