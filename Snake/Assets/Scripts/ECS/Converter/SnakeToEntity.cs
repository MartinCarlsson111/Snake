using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Physics;


public class SnakeToEntity : MonoBehaviour
{
    EntityManager em;
    EntityArchetype snakeBodyArchetype;
    EntityArchetype snakeHeadArchetype;

    public Mesh snakeHeadMesh;
    public Mesh snakeBodyMesh;

    public UnityEngine.Material snakeHeadMaterial;
    public UnityEngine.Material snakeBodyMaterial;
    public int nSize = 5;

    public float mass = 1.0f;
    public float angularVelocity = 0.0f;
    public float linearVelocity = 0.0f;
    public float linearDamping = 0.01f;
    public float angularDamping = 0.05f;

    void CreateArchetypes()
    {

        ComponentType[] physicsComponents = {typeof(PhysicsCollider),
            typeof(PhysicsVelocity),
            typeof(PhysicsMass),
            typeof(PhysicsDamping),
            typeof(TranslationProxy),
            typeof(RotationProxy)};

        ComponentType[] snakeBodyComponentTypes =
        {
            typeof(SnakeBody), typeof(Translation), typeof(Rotation), typeof(RenderMesh),
            typeof(LocalToWorld)
        };

        ComponentType[] snakeHeadComponentTypes =
        {
            typeof(SnakeHead), typeof(Translation), typeof(RenderMesh), typeof(Rotation), typeof(LocalToWorld), typeof(SnakeBodyBuffer), typeof(MovementSpeed)
        };


        ComponentType[] snakeBodyWithPhysics = new ComponentType[snakeBodyComponentTypes.Length + physicsComponents.Length];
        System.Array.Copy(snakeBodyComponentTypes, snakeBodyWithPhysics, snakeBodyComponentTypes.Length);
        System.Array.Copy(physicsComponents, 0, snakeBodyWithPhysics, snakeBodyComponentTypes.Length, physicsComponents.Length);


        ComponentType[] snakeHeadWithPhysics = new ComponentType[snakeHeadComponentTypes.Length + physicsComponents.Length];
        System.Array.Copy(snakeHeadComponentTypes, snakeHeadWithPhysics, snakeHeadComponentTypes.Length);
        System.Array.Copy(physicsComponents, 0, snakeHeadWithPhysics, snakeHeadComponentTypes.Length, physicsComponents.Length);

        snakeBodyArchetype = em.CreateArchetype(snakeBodyWithPhysics);
        snakeHeadArchetype = em.CreateArchetype(snakeHeadWithPhysics);

    }

    unsafe void Start()
    {
        em = World.Active.EntityManager;
        CreateArchetypes();
        float3[] vertices = new float3[snakeHeadMesh.vertices.Length];

        for (int i = 0; i < snakeHeadMesh.vertices.Length; i++)
        {
            vertices[i] = snakeHeadMesh.vertices[i];
        }

        float3[] bodyVertices = new float3[snakeBodyMesh.vertices.Length];

        for (int i = 0; i < snakeBodyMesh.vertices.Length; i++)
        {
            bodyVertices[i] = snakeBodyMesh.vertices[i];
        }

        var CollidesWith = ~0u;
        var belongsTo = 1u << 1;
        BlobAssetReference<Unity.Physics.Collider> snakeHeadCollider = Unity.Physics.MeshCollider.Create(vertices, snakeHeadMesh.triangles, new CollisionFilter() { BelongsTo = belongsTo, CollidesWith = CollidesWith, GroupIndex = 0 });
        BlobAssetReference<Unity.Physics.Collider> snakeBodyCollider = Unity.Physics.MeshCollider.Create(vertices, snakeHeadMesh.triangles, new CollisionFilter() { BelongsTo = belongsTo, CollidesWith = CollidesWith, GroupIndex = 0 });

        float3 startPos = transform.position;
        var snakeEntity = em.CreateEntity(snakeHeadArchetype);
        var snakePos = startPos;
        var snakeRotation = quaternion.identity;
        em.SetComponentData(snakeEntity, new SnakeHead() { size = 1.0f});
        em.SetComponentData(snakeEntity, new Translation() { Value = snakePos });
        em.SetComponentData(snakeEntity, new Rotation() { Value = snakeRotation });
        em.SetComponentData(snakeEntity, new LocalToWorld() { });
        em.SetComponentData(snakeEntity, new MovementSpeed() { value = 4.0f });
        em.SetComponentData(snakeEntity, new PhysicsCollider() { Value = snakeBodyCollider });
        Unity.Physics.Collider* colliderPtr = (Unity.Physics.Collider*)snakeBodyCollider.GetUnsafePtr();
        em.SetComponentData(snakeEntity, PhysicsMass.CreateDynamic(colliderPtr->MassProperties, mass));

        float3 angularVelocityLocal = math.mul(math.inverse(colliderPtr->MassProperties.MassDistribution.Transform.rot), angularVelocity);

        em.SetComponentData(snakeEntity, new PhysicsVelocity(){ Linear = linearVelocity, Angular = angularVelocityLocal });
        em.SetComponentData(snakeEntity, new PhysicsDamping() { Linear = linearDamping, Angular = angularDamping });
        em.SetSharedComponentData(snakeEntity, new RenderMesh() { mesh = snakeHeadMesh, material = snakeHeadMaterial, castShadows = UnityEngine.Rendering.ShadowCastingMode.On, receiveShadows = true });
        var buffer = em.AddBuffer<SnakeBodyBuffer>(snakeEntity);

        for (int i = 0; i < nSize; i++)
        {
            var snakeBodyPos = startPos;
            var snakeBodyRotation = quaternion.identity;
            buffer.Add(new SnakeBodyBuffer() { position = snakeBodyPos, rotation = snakeBodyRotation });
        }

        for (int i = 0; i < nSize; i++)
        {
            var snakeBodyEntity = em.CreateEntity(snakeBodyArchetype);
            var snakeBodyPos = startPos;
            var snakeBodyRotation = quaternion.identity;

            em.SetComponentData(snakeBodyEntity, new SnakeBody() { index = i, entity = snakeEntity, size = 1.0f, target = snakeBodyPos});
            em.SetComponentData(snakeBodyEntity, new Translation() { Value = snakeBodyPos });
            em.SetComponentData(snakeBodyEntity, new Rotation() { Value = snakeBodyRotation });
            em.SetComponentData(snakeBodyEntity, new LocalToWorld() { });
            em.SetComponentData(snakeBodyEntity, new PhysicsCollider() { Value = snakeBodyCollider });
            Unity.Physics.Collider* colliderPtr2 = (Unity.Physics.Collider*)snakeHeadCollider.GetUnsafePtr();
            em.SetComponentData(snakeBodyEntity, PhysicsMass.CreateDynamic(colliderPtr2->MassProperties, mass));
            em.SetComponentData(snakeBodyEntity, new PhysicsVelocity() { Linear = linearVelocity, Angular = angularVelocityLocal });
            em.SetComponentData(snakeBodyEntity, new PhysicsDamping() { Linear = linearDamping, Angular = angularDamping });
            em.SetSharedComponentData(snakeBodyEntity, new RenderMesh() { mesh = snakeBodyMesh, material = snakeBodyMaterial, castShadows = UnityEngine.Rendering.ShadowCastingMode.On, receiveShadows = true });
        }
    }
}
