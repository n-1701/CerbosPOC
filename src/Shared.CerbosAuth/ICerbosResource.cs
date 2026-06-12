namespace Shared.CerbosAuth;

/// <summary>
/// Implement this on any domain object you want to authorize.
/// The handler reads these properties to build the Cerbos request.
/// Policy references: resource.attr.ownerId, resource.attr.department, + ExtraAttributes keys.
/// </summary>
public interface ICerbosResource
{
    string ResourceId  { get; }  // unique ID of this instance
    string OwnerId     { get; }  // maps to resource.attr.ownerId in policy
    string Department  { get; }  // maps to resource.attr.department in policy

    // Any other attributes the policy conditions need (e.g. status, isPublished)
    Dictionary<string, object> ExtraAttributes { get; }
}
