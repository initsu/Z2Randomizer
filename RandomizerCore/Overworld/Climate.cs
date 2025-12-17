using System;
using System.Collections.Generic;
using System.Linq;

namespace Z2Randomizer.RandomizerCore.Overworld;

public class Climate
{
    public float[] DistanceCoefficients { get; }
    public string Name { get; init; }
    public int SeedTerrainCount { get; set; }
    private IWeightedSampler<Terrain> weightedSampler;

    public Climate(string name, Dictionary<Terrain, float> distanceCoefficients, IWeightedSampler<Terrain> terrainWeights, int seedTerrainCount, bool providedDistancesAreInverted = false)
    {
        Name = name;
        // Precompute distance coefficients into a inverse list
        DistanceCoefficients = new float[(int)Terrain.NONE];
        foreach (var pair in distanceCoefficients)
        {
            DistanceCoefficients[(int)pair.Key] = providedDistancesAreInverted ? pair.Value : (1f / pair.Value);
        }
        weightedSampler = terrainWeights;
        SeedTerrainCount = seedTerrainCount;
    }

    public Terrain GetRandomTerrain(Random r, IEnumerable<Terrain> whitelist)
    {
        Terrain result;
        do
        {
            result = weightedSampler.Next(r);
        }
        while (whitelist == null || !whitelist.Contains(result));

        return result;
    }

    public IEnumerable<Terrain> RandomTerrains(IEnumerable<Terrain> filter)
    {
        if (filter == null)
        {
            return weightedSampler.Keys();
        }
        return filter.Intersect(weightedSampler.Keys());
    }

    public void DisallowTerrain(Terrain terrain)
    {
        weightedSampler = weightedSampler.Remove(terrain);
    }

    public Climate Clone()
    {
        return new(Name,
            new Dictionary<Terrain, float>(DistanceCoefficients
                .Select((value, index) => new { value, index })
                .ToDictionary(pair => (Terrain)pair.index, pair => pair.value)),
            weightedSampler.Clone(),
            SeedTerrainCount, providedDistancesAreInverted: true);
    }

    /// <summary>
    /// If death mountain has distance coefficients that are too small relative to the growth of mountains,
    /// there isn't ever enough open space to walk around between caves and it never properly generates.
    /// In order to resolve that, if the aggregate weighted terrain growth factor is less than that of mountain
    /// (times the factor provided in constraintLimitFactor), increase the growth factor of each terrain type
    /// by the relative difference between mountain terrain growth and that terrain growth.
    /// </summary>
    /// <param name="walkableTerrains">Types of terrains that can be randomly generated that need to be scaled.</param>
    /// <param name="constraintLimitFactor">The relative permissible factor between walkable and mountain terrain growths</param>
    public void ApplyDeathMountainSafety(IEnumerable<Terrain> walkableTerrains, float constraintLimitFactor = 1f)
    {
        float coefficientSum = 0f;
        foreach (Terrain terrain in walkableTerrains)
        {
            if(terrain != Terrain.ROAD)
            {
                coefficientSum += DistanceCoefficients[(int)terrain];
            }
        }
        float averageCoefficient = coefficientSum / (walkableTerrains.Count() - walkableTerrains.Count(i => i == Terrain.ROAD));
        DistanceCoefficients[(int)Terrain.ROAD] = float.Min(DistanceCoefficients[(int)Terrain.ROAD], averageCoefficient);

        float totalRandomGrowthFactors = 0f;
        float totalWalkableTerrainWeight = 0f;
        foreach (Terrain terrain in walkableTerrains)
        {
            var w = weightedSampler.Weight(terrain);
            totalRandomGrowthFactors += w / DistanceCoefficients[(int)terrain];
            totalWalkableTerrainWeight += w;
        }
        float aggregateWalkableTerrainGrowth = totalRandomGrowthFactors / totalWalkableTerrainWeight;
        float mountainTerrainGrowth = 1 / DistanceCoefficients[(int)Terrain.MOUNTAIN];

        if (mountainTerrainGrowth * constraintLimitFactor > aggregateWalkableTerrainGrowth)
        {
            foreach (Terrain terrain in walkableTerrains)
            {
                DistanceCoefficients[(int)terrain] /= ((mountainTerrainGrowth * constraintLimitFactor) / aggregateWalkableTerrainGrowth);
            }
        }
    }
}

