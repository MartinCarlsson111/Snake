using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;

public class MovementSystem : JobComponentSystem
{
    float updateAccu = 0.0f;
    float updateTimer = 0.25f;

    public struct RotateJob : IJobForEach<SnakeHead, Rotation>
    {
        [ReadOnly]public float2 inputAxis;
        public void Execute(ref SnakeHead c0, ref Rotation c1)
        {
            var forward = math.forward(c1.Value);
            if (!(forward.z < 0.0005f && forward.z > -0.0005f) && inputAxis.x != 0)
            {
                c1.Value = quaternion.LookRotation(new float3(inputAxis.x, 0, 0), new float3(0, 1, 0));
            }
            if (forward.x != 0 && inputAxis.y != 0)
            {
                c1.Value = quaternion.LookRotation(new float3(0, 0, inputAxis.y), new float3(0, 1, 0));
            }
        }
    }

    [BurstCompile]
    public struct MovementJob : IJobForEachWithEntity<SnakeHead, Translation, Rotation, MovementSpeed, Unity.Physics.PhysicsVelocity>
    {
        [ReadOnly] public float dt;
        [ReadOnly] public float updateTimer;
        [NativeDisableParallelForRestriction] public BufferFromEntity<SnakeBodyBuffer> snakeBodyBuffer;
        public float accu;
        public void Execute(Entity entity, int index, ref SnakeHead c0, ref Translation c1, ref Rotation c2, ref MovementSpeed c3, ref Unity.Physics.PhysicsVelocity c4)
        {
            float3 lastPos = c1.Value;
            var forward = math.forward(c2.Value);
            c4.Linear = forward * dt * c3.value * 10;
            if (accu >= updateTimer)
            {
                if (snakeBodyBuffer[entity].Length > 0)
                {
                    snakeBodyBuffer[entity].RemoveAt(snakeBodyBuffer[entity].Length - 1);
                    snakeBodyBuffer[entity].Insert(0, new SnakeBodyBuffer()
                    {
                        position = lastPos,
                        rotation = c2.Value
                    });
                }
            }
        }
    }

    [BurstCompile]
    public struct UpdateBodyJob : IJobForEachWithEntity<SnakeBody, Translation, Rotation>
    {
        public float dt;
        public EntityCommandBuffer.Concurrent cmd;
        [ReadOnly] [NativeDisableParallelForRestriction] public BufferFromEntity<SnakeBodyBuffer> snakeBodyBuffer;
        public void Execute(Entity entity, int index, ref SnakeBody c0, ref Translation c1, ref Rotation c2)
        {
            var diff = c0.target - c1.Value;
            c1.Value = diff * dt * 10;
            var res = (diff.x < c0.size && diff.x > -c0.size) && (diff.z < c0.size && diff.z > -c0.size);

            if(res)
            {
                if (snakeBodyBuffer[c0.entity].Length > c0.index)
                {
                    c0.target = snakeBodyBuffer[c0.entity][c0.index].position /*- (c0.size * (c0.index +1) ) * math.forward(snakeBodyBuffer[c0.entity][c0.index].rotation)*/;
                    c2.Value = snakeBodyBuffer[c0.entity][c0.index].rotation;
                }
                else
                {
                    cmd.DestroyEntity(index, entity);
                }
            }
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        updateAccu += Time.deltaTime;
        float2 inputAxis = new float2(0, 0);
        if(Input.GetKey(KeyCode.A))
        {
            inputAxis.x -= 1;
        }
        if(Input.GetKey(KeyCode.D))
        {
            inputAxis.x += 1;
        }

        if(Input.GetKey(KeyCode.S))
        {
            inputAxis.y -= 1;
        }

        if(Input.GetKey(KeyCode.W))
        {
            inputAxis.y += 1;
        }

        var rotationJob = new RotateJob()
        {
            inputAxis = inputAxis,
        };
        var rotationJobHandle = rotationJob.Schedule(this, inputDeps);

        var movementJob = new MovementJob()
        {
            snakeBodyBuffer = GetBufferFromEntity<SnakeBodyBuffer>(),
            dt = Time.deltaTime,
            accu = updateAccu,
            updateTimer = updateTimer
        };

        var movementJobHandle = movementJob.Schedule(this, rotationJobHandle);
        EntityCommandBuffer cmd = new EntityCommandBuffer(Allocator.TempJob);
        var concurrentCmd = cmd.ToConcurrent();
        var updateBodyJob = new UpdateBodyJob()
        {

            cmd = concurrentCmd,
            snakeBodyBuffer = GetBufferFromEntity<SnakeBodyBuffer>(true),
            dt = Time.deltaTime,
        };

        var updateJobHandle = updateBodyJob.Schedule(this, movementJobHandle);

        updateJobHandle.Complete();

        cmd.Playback(World.Active.EntityManager);
        cmd.Dispose();

        if(updateAccu >= updateTimer)
        {
            updateAccu = 0;
        }

        return updateJobHandle;
    }
}
