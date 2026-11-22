using System;

namespace Z2Randomizer.RandomizerCore.Diagnostics;

/// <summary>
/// Generates partial property implementations that reject reads before assignment in Debug builds.
/// Properties with explicit declaration defaults can remain ordinary auto-properties.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class CheckedInitializationAttribute : Attribute;
