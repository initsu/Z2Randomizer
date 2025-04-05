﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RandomizerCore.Sidescroll;

namespace RandomizerCore;

public class Requirements
{
    public RequirementType[] IndividualRequirements { get; private set; }
    public RequirementType[][] CompositeRequirements { get; private set; }

    public Requirements()
    {
        IndividualRequirements = [];
        CompositeRequirements = [];
    }

    public Requirements(RequirementType[] requirements) : this()
    {
        IndividualRequirements = requirements;
    }

    public Requirements(RequirementType[] requirements, RequirementType[][] compositeRequirements) : this()
    {
        IndividualRequirements = requirements;
        CompositeRequirements = compositeRequirements;
    }

    public Requirements(string? json)
    {
        var n = Deserialize(json);
        IndividualRequirements = n?.IndividualRequirements ?? [];
        CompositeRequirements = n?.CompositeRequirements ?? [];
    }

    public Requirements? Deserialize(string? json)
    {
        return JsonSerializer.Deserialize(json ?? "[]", RoomSerializationContext.Default.Requirements);
    }
    
    public string Serialize()
    {
        StringBuilder sb = new();
        sb.Append('[');
        foreach (var t in IndividualRequirements)
        {
            sb.Append('"');
            sb.Append(t.ToString());
            sb.Append('"');
            sb.Append(',');
        }
        foreach (var t in CompositeRequirements)
        {
            sb.Append('[');
            foreach (var t1 in t)
            {
                sb.Append('"');
                sb.Append(t1.ToString());
                sb.Append('"');
                sb.Append(',');
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(']');
        }
        if (sb.Length > 1 && sb[^1] != ']')
        {
            sb.Remove(sb.Length - 1, 1);
        }
        sb.Append(']');

        return sb.ToString();
    }

    public override string ToString()
    {
        return Serialize();
    }

    public bool AreSatisfiedBy(IEnumerable<RequirementType> requireables)
    {

        var individualRequirementsSatisfied = false;
        var requirementTypes = requireables as RequirementType[] ?? requireables.ToArray();
        foreach (var requirement in IndividualRequirements)
        {
            if (requirementTypes.Contains(requirement))
            {
                individualRequirementsSatisfied = true;
                break;
            }
        }

        if(IndividualRequirements.Length > 0 && !individualRequirementsSatisfied)
        {
            return false;
        }

        var compositeRequirementSatisfied = 
            CompositeRequirements.Length == 0 || CompositeRequirements.Any(compositeRequirement =>
            compositeRequirement.All(i => requirementTypes.Contains(i)));
        return (IndividualRequirements.Length > 0 && individualRequirementsSatisfied) || compositeRequirementSatisfied;
    }

    public Requirements AddHardRequirement(RequirementType requirement)
    {
        Requirements newRequirements = new();
        //if no requirements return a single requirement of the type
        if(IndividualRequirements.Length == 0 && CompositeRequirements.Length == 0)
        {
            newRequirements.IndividualRequirements = [requirement];
            return newRequirements;
        }
        newRequirements.CompositeRequirements = new RequirementType[CompositeRequirements.Length + IndividualRequirements.Length][];
        //all individual requirements become composite requirements containing the type
        for(int i = 0; i < IndividualRequirements.Length; i++)
        {
            newRequirements.CompositeRequirements[i] = [IndividualRequirements[i], requirement];
        }
        //all composite requirements not containing the type now contain the type
        for (int i = 0; i < CompositeRequirements.Length; i++)
        {
            newRequirements.CompositeRequirements[i + IndividualRequirements.Length] = [.. CompositeRequirements[i], requirement];
        }
        return newRequirements;
    }

    public bool HasHardRequirement(RequirementType requireable)
    {
        if (IndividualRequirements.Any(requirement => requirement != requireable))
        {
            return false;
        }
        foreach (var compositeRequirement in CompositeRequirements)
        {
            var containsRequireable = false;
            foreach (var requirement in compositeRequirement)
            {
                if (requirement != requireable)
                {
                    containsRequireable = true;
                }
            }
            if(!containsRequireable)
            {
                return false;
            }
        }
        return true;
    }
}

public class RequirementsJsonConverter : JsonConverter<Requirements>
{
    public override Requirements Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            return new Requirements();
        List<RequirementType> individualReqs = [];
        List<List<RequirementType>> compositeReqs = [];
        var doc = JsonDocument.ParseValue(ref reader);
        foreach (var req in doc.RootElement.EnumerateArray())
        {
            switch (req.ValueKind)
            {
                case JsonValueKind.String:
                    individualReqs.Add((RequirementType)Enum.Parse(typeof(RequirementType), req.ToString()));
                    break;
                case JsonValueKind.Array:
                    List<RequirementType> newComp = [];
                    compositeReqs.Add(newComp);
                    var subArray = req.EnumerateArray();
                    var comps = subArray.Select(comp =>
                        (RequirementType)Enum.Parse(typeof(RequirementType), comp.ToString()));
                    newComp.AddRange(comps);
                    break;
            }
        }
        return new Requirements(individualReqs.ToArray(),
            compositeReqs.Select(i => i.ToArray()).ToArray());
    }

    public override void Write(Utf8JsonWriter writer, Requirements value, JsonSerializerOptions options)
    {
        writer.WriteRawValue(value?.Serialize() ?? "[]");
    }
}
