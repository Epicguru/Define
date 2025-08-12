using JetBrains.Annotations;

namespace Define;

/// <summary>
/// The base interface that all Defs must implement.
/// It declares a string <see cref="ID"/> that is globally unique among all defs.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature | ImplicitUseKindFlags.Assign, 
                ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors)]
public interface IDef
{
    /// <summary>
    /// The unique ID of this def.
    /// </summary>
    string ID { get; set; }
}