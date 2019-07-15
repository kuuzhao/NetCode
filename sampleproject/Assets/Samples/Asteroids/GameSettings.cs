using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public struct GameSettingsComponent : IComponentData
{
}

public class GameSettings : UnityEngine.MonoBehaviour, IConvertGameObjectToEntity
{
    public float asteroidRadius = 15f;
    public float playerRadius = 10f;
    public float bulletRadius = 1f;

    public float asteroidVelocity = 10f;
    public float playerForce = 50f;
    public float bulletVelocity = 500f;

    public int numAsteroids = 200;
    public int levelWidth = 2048;
    public int levelHeight = 2048;
    public int damageShips = 1;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new GameSettingsComponent();
        dstManager.AddComponentData(entity, data);
        var settings = default(ServerSettings);
        settings.asteroidRadius = asteroidRadius;
        settings.playerRadius = playerRadius;
        settings.bulletRadius = bulletRadius;

        settings.asteroidVelocity = asteroidVelocity;
        settings.playerForce = playerForce;
        settings.bulletVelocity = bulletVelocity;

        settings.numAsteroids = numAsteroids;
        settings.levelWidth = levelWidth;
        settings.levelHeight = levelHeight;
        settings.damageShips = damageShips;
        dstManager.AddComponentData(entity, settings);
    }
}
