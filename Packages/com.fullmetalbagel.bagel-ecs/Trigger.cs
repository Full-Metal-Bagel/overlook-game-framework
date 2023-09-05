using System;
using System.Collections.Generic;

namespace RelEcs
{
    internal sealed class Trigger<T>
    {
        internal T Value = default!;
    }

    internal sealed class SystemList
    {
        public readonly List<Type> List;
        public SystemList() => List = ListPool<Type>.Get();
    }

    internal sealed class LifeTime
    {
        public int Value;
    }

    internal sealed class TriggerLifeTimeSystem : ISystem
    {
        public void Run(World world)
        {
            var query = world.Query<Entity, SystemList, LifeTime>().Build();
            foreach (var (entity, systemList, lifeTime) in query)
            {
                lifeTime.Value++;

                if (lifeTime.Value < 2) return;

                ListPool<Type>.Add(systemList.List);
                world.Despawn(entity);
            }
        }
    }
}
