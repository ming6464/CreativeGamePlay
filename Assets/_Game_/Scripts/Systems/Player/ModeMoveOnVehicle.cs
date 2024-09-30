using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace _Game_.Scripts.Systems.Player
{
    public partial struct ModeMoveOnVehicle : ISystem
    {
        private NativeArray<bufferMoveDestination> _bufferMoveDestinations;
        private Entity _playerInfoEntity;
        private bool _init;
        private EntityManager _entityManager;
        private float3 _nextDestination;
        private float _speed;
        private int _nextIndexDestination;
        private bool _startPosition;
        private bool _onMode;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            RequiredNecessary(ref state);
        }

        [BurstCompile]
        private void RequiredNecessary(ref SystemState state)
        {
            state.RequireForUpdate<PlayerProperty>();
            state.RequireForUpdate<PlayerInfo>();
            _onMode = true;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if(!CheckAndInit(ref state) || !_onMode) return;
            Move(ref state);
        }

        private void Move(ref SystemState state)
        {
            if (_bufferMoveDestinations.Length == 0 || _nextIndexDestination >= _bufferMoveDestinations.Length)
            {
                state.Enabled = false;
                return;
            }
            var positionWorld = SystemAPI.GetComponentRO<LocalToWorld>(_playerInfoEntity).ValueRO.Position;
            var deltaTime = SystemAPI.Time.DeltaTime;
            var lt = SystemAPI.GetComponentRW<LocalTransform>(_playerInfoEntity);
            positionWorld = lt.ValueRO.Position;
            if (!_startPosition)
            {
                _startPosition = true;
                positionWorld = _bufferMoveDestinations[0].position;
                _nextDestination = _bufferMoveDestinations[_nextIndexDestination].position;
                _speed = _bufferMoveDestinations[_nextIndexDestination].speed;
            }
            
            var nextPos = MathExt.MoveTowards(positionWorld, _nextDestination, _speed * deltaTime);
            if (nextPos.ComparisionEqual(_nextDestination))
            {
                _nextIndexDestination++;
                if (_nextIndexDestination < _bufferMoveDestinations.Length)
                {
                    _nextDestination = _bufferMoveDestinations[_nextIndexDestination].position;
                    _speed = _bufferMoveDestinations[_nextIndexDestination].speed;
                }
            }
            lt.ValueRW.Position = nextPos;
        }

        [BurstCompile]
        private bool CheckAndInit(ref SystemState state)
        {
            if (_init) return true;
            _entityManager = state.EntityManager;
            _playerInfoEntity = SystemAPI.GetSingletonEntity<PlayerInfo>();
            var entityPlayerProperty = SystemAPI.GetSingletonEntity<PlayerProperty>();

            var playerProperty = SystemAPI.GetComponentRO<PlayerProperty>(entityPlayerProperty);
            _onMode = playerProperty.ValueRO.helicopterMode;
            state.Enabled = _onMode;
            if (!_onMode) return false;
            _bufferMoveDestinations = _entityManager.GetBuffer<bufferMoveDestination>(entityPlayerProperty)
                .ToNativeArray(Allocator.Persistent);
            if (_bufferMoveDestinations.Length > 1)
            {
                _nextIndexDestination = 1;
            }
            else
            {
                state.Enabled = false;
            }
            _init = true;
            return false;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_bufferMoveDestinations.IsCreated)
                _bufferMoveDestinations.Dispose();
        }
    }
}