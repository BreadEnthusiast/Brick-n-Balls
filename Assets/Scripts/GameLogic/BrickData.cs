using System;
using Unity.Entities;

namespace BrickNBalls.GameLogic
{
    /// <summary>
    /// Represents the game state data for a single brick.
    /// This is a plain C# class (OOP) that holds game logic state,
    /// separate from the ECS physics entity.
    /// </summary>
    public sealed class BrickData
    {
        /// <summary>
        /// The ECS entity associated with this brick.
        /// </summary>
        public Entity Entity { get; }

        /// <summary>
        /// The current health points of the brick.
        /// </summary>
        public int Health { get; private set; }

        /// <summary>
        /// The maximum health points the brick started with.
        /// </summary>
        public int MaxHealth { get; }

        /// <summary>
        /// Whether the brick has been destroyed (health reached zero).
        /// </summary>
        public bool IsDestroyed => Health <= 0;

        /// <summary>
        /// Event raised when this brick takes damage.
        /// Parameters: (BrickData brick, int damageAmount, int remainingHealth)
        /// </summary>
        public event Action<BrickData, int, int> DamageTaken;

        /// <summary>
        /// Event raised when this brick is destroyed.
        /// </summary>
        public event Action<BrickData> Destroyed;

        /// <summary>
        /// Creates a new BrickData instance.
        /// </summary>
        /// <param name="entity">The ECS entity associated with this brick.</param>
        /// <param name="initialHealth">The starting health of the brick.</param>
        public BrickData(Entity entity, int initialHealth)
        {
            Entity = entity;
            Health = initialHealth;
            MaxHealth = initialHealth;
        }

        /// <summary>
        /// Applies damage to the brick.
        /// </summary>
        /// <param name="damage">The amount of damage to apply.</param>
        /// <returns>True if the brick was destroyed by this damage, false otherwise.</returns>
        public bool TakeDamage(int damage)
        {
            if (IsDestroyed)
            {
                return false;
            }

            if (damage <= 0)
            {
                return false;
            }

            Health -= damage;
            DamageTaken?.Invoke(this, damage, Health);

            if (Health <= 0)
            {
                Health = 0;
                Destroyed?.Invoke(this);
                return true;
            }

            return false;
        }
    }
}
