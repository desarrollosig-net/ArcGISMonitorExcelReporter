# Resource Fields Usage Guide

## Overview

The Resource Fields feature allows dynamic verification and validation of API schema during report generation. Instead of relying on hardcoded field lists, the system now queries the `/monitoring/{resource}` endpoints to obtain actual field information from the ArcGIS Monitor API.

## What Are Resource Fields?

Resource fields are obtained from the `/monitoring` endpoint and contain:

1. **Resource List**: Available resources (e.g., "metrics", "alerts", "components", "collections", etc.)
2. **Schema Information**: For each resource, field definitions including:
   - Field name
   - Field type (string, number, date, enum, etc.)
   - Field description
   - Whether the field is required

## Data Flow

```
GET /monitoring
  ↓
[List of resources: metrics, alerts, components, ...]
  ↓
for each resource →
  GET /monitoring/{resource}
    ↓
    [Field definitions and schema]
  ↓
Stored in MonitorExcelReport.ResourceFields
  ↓
Available during query execution for validation
  ↓
Used to enrich data with field metadata
```

## Using Resource Fields in Queries

### 1. Validating Available Fields

Before executing a query for a resource, use `ArcGisMonitorQueryService.GetAvailableFields()`:

```csharp
var availableFields = ArcGisMonitorQueryService.GetAvailableFields(
    resourceName: "metrics",
    requestedFields: new[] { "id", "name", "last_value", "custom_field" },
    resourceFields: report.ResourceFields
);

// Result: ["id", "name", "last_value"]  <- custom_field filtered out
```

### 2. Getting Resource Information

To access field details for a specific resource:

```csharp
var resourceInfo = ArcGisMonitorQueryService.GetResourceInfo("metrics", report.ResourceFields);

// Access field details
if(resourceInfo?.Fields != null)
{
    foreach(var field in resourceInfo.Fields)
    {
        if(field.Required == true)
        {
            // Handle required fields specially
        }
    }
}
```

### 3. Dynamic Field Selection

Use available fields to dynamically build queries:

```csharp
public IEnumerable<string> BuildOptionalFields(
    string resourceName,
    Dictionary<string, ResourceFieldInfo> resourceFields)
{
    // These are fields we'd LIKE to include if available
    var desiredFields = new[] 
    { 
        "custom_metadata", 
        "extended_info", 
        "tags" 
    };

    // Filter to only those that actually exist
    return ArcGisMonitorQueryService.GetAvailableFields(
        resourceName, 
        desiredFields, 
        resourceFields
    );
}
```

## Implementation in Query Services

When extending query functionality, follow this pattern:

```csharp
// 1. Get field information
var resourceInfo = ArcGisMonitorQueryService.GetResourceInfo(
    "your_resource", 
    report.ResourceFields
);

if(resourceInfo?.Fields == null)
{
    Log.Warning("No field information available for '{Resource}'", "your_resource");
    return; // or handle gracefully
}

// 2. Validate requested fields
var validatedFields = ArcGisMonitorQueryService.GetAvailableFields(
    "your_resource",
    requestedFields,
    report.ResourceFields
);

// 3. Use validated fields in query
// ... execute query with validatedFields
```

## Benefits

1. **Dynamic Schema**: Adapts to API changes without code modification
2. **Error Prevention**: Prevents invalid field errors before query execution
3. **Field Discovery**: Enables tools to discover available fields at runtime
4. **Documentation**: API schema is embedded in the report
5. **Validation**: Automatic field validation before query execution

## Example: Enhanced Component Query

```csharp
// Old way - hardcoded fields
var query = new ComponentQuery { OutFields = "*" };

// New way - validated fields
var availableFields = ArcGisMonitorQueryService.GetAvailableFields(
    "components",
    null, // null = get all available
    report.ResourceFields
);

Log.Information("Component query will include {FieldCount} fields: {Fields}",
    availableFields.Count,
    string.Join(", ", availableFields));

var query = new ComponentQuery { OutFields = availableFields };
```

## Caching

Resource fields are:
- Retrieved once during `ArcGISMonitorExcelReporter.BuildReportAsync()`
- Stored in `MonitorExcelReport.ResourceFields` dictionary
- Available throughout the entire report generation process
- Can be accessed by any query service method

## Performance Considerations

- Resource fields are fetched in parallel using `GetAllResourceFieldsAsync()`
- ~15-20 resources × 1 HTTP request per resource = good parallelization opportunity
- Fields are cached in memory and reused across all queries
- No runtime impact on query execution - only used for validation/metadata

## Future Extensions

Consider extending this feature to:
1. Auto-generate field documentation
2. Create dynamic Excel templates based on available fields
3. Implement field-level filtering policies
4. Generate API client code based on available fields
5. Track field changes across API versions
