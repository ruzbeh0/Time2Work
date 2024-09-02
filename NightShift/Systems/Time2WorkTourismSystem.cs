// Decompiled with JetBrains decompiler
// Type: Game.Simulation.Time2WorkTourismSystem
// Assembly: Game, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 3C8C3C1D-D7EB-4536-8BE0-6F4028D2725F
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data\Managed\Game.dll

using Game;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

#nullable disable
namespace Time2Work.Systems
{
    public partial class Time2WorkTourismSystem : GameSystemBase
    {
        private int2 m_CachedLodging;
        private CitySystem m_CitySystem;
        private CityStatisticsSystem m_CityStatisticsSystem;
        private ClimateSystem m_ClimateSystem;
        private EntityQuery m_AttractionGroup;
        private EntityQuery m_TouristHouseholdGroup;
        private EntityQuery m_LodgingGroup;
        private EntityQuery m_ParameterQuery;
        private Time2WorkTourismSystem.TypeHandle __TypeHandle;
        private const float weekdayTourismFactor = 0.9f;
        private const float weekendTourismFactor = 1.15f;
        private Setting.DTSimulationEnum m_daytype;

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 32768;

        public static int GetTouristRandomStay() => 262144;

        public static float GetRawTouristProbability(int attractiveness)
        {
            return (float)attractiveness / 1000f;
        }

        public static float GetTouristProbability(
          AttractivenessParameterData parameterData,
          int attractiveness,
          float temperature,
          float raininess,
          float cloudiness,
          Setting.DTSimulationEnum dayType)
        {
            float prob = Time2WorkTourismSystem.GetRawTouristProbability(attractiveness) * Time2WorkTourismSystem.GetWeatherEffect(parameterData, temperature, raininess, cloudiness);
            
            if(((int)dayType) == (int)Setting.DTSimulationEnum.Weekday)
            {
                prob *= weekdayTourismFactor;
            } else if (((int)dayType) == (int)Setting.DTSimulationEnum.AverageDay)
            {
                prob *= (weekendTourismFactor+ weekdayTourismFactor)/2;
            } else
            {
                prob *= weekendTourismFactor;
            }

            return prob;
        }

        public static float GetWeatherEffect(
          AttractivenessParameterData parameterData,
          float temperature,
          float raininess,
          float cloudiness)
        {
            return math.clamp(math.clamp((float)(0.30000001192092896 + (double)math.abs(temperature - 10f) / 15.0), 1f, 1.5f) * math.clamp((float)(-(double)math.abs(raininess - parameterData.m_RaininessBaseline) / 0.30000001192092896 + 1.2000000476837158), parameterData.m_RaininessAffectLimit.x, parameterData.m_RaininessAffectLimit.y) * math.clamp((float)(-(double)math.abs(cloudiness - parameterData.m_CloudinessBaseline) / 0.5 + 1.2000000476837158), parameterData.m_CloudinessAffectLimit.x, parameterData.m_CloudinessAffectLimit.y), 0.5f, 1.5f);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
            this.m_CityStatisticsSystem = this.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            this.m_ClimateSystem = this.World.GetOrCreateSystemManaged<ClimateSystem>();
            this.m_AttractionGroup = this.GetEntityQuery(ComponentType.ReadWrite<AttractivenessProvider>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
            this.m_TouristHouseholdGroup = this.GetEntityQuery(ComponentType.ReadOnly<TouristHousehold>(), ComponentType.ReadOnly<HouseholdCitizen>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
            this.m_LodgingGroup = this.GetEntityQuery(ComponentType.ReadOnly<LodgingProvider>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
            this.m_ParameterQuery = this.GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
            if (Mod.m_Setting.tourism_trips)
            {
                this.m_daytype = WeekSystem.currentDayOfTheWeek;
            }
            else
            {
                this.m_daytype = Setting.DTSimulationEnum.AverageDay;
            }
        }

        protected override void OnUpdate()
        {
            this.__TypeHandle.__Game_Buildings_AttractivenessProvider_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_City_Tourism_RW_ComponentLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Companies_LodgingProvider_RO_ComponentTypeHandle.Update(ref this.CheckedStateRef);
            this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle.Update(ref this.CheckedStateRef);
            if(Mod.m_Setting.tourism_trips)
            {
                this.m_daytype = WeekSystem.currentDayOfTheWeek;
            } else
            {
                this.m_daytype = Setting.DTSimulationEnum.AverageDay;
            }
            
            JobHandle deps;

            Time2WorkTourismSystem.TourismJob jobData = new Time2WorkTourismSystem.TourismJob()
            {
                m_Chunks = this.m_AttractionGroup.ToArchetypeChunkArray((AllocatorManager.AllocatorHandle)Allocator.TempJob),
                m_TouristHouseholdChunks = this.m_TouristHouseholdGroup.ToArchetypeChunkArray((AllocatorManager.AllocatorHandle)Allocator.TempJob),
                m_HotelChunks = this.m_LodgingGroup.ToArchetypeChunkArray((AllocatorManager.AllocatorHandle)Allocator.TempJob),
                m_HouseholdCitizenType = this.__TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle,
                m_LodgingProviderType = this.__TypeHandle.__Game_Companies_LodgingProvider_RO_ComponentTypeHandle,
                m_RenterType = this.__TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle,
                m_CityModifiers = this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup,
                m_Tourisms = this.__TypeHandle.__Game_City_Tourism_RW_ComponentLookup,
                m_Parameters = this.m_ParameterQuery.GetSingleton<AttractivenessParameterData>(),
                m_StatisticsEventQueue = this.m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter(),
                m_ProviderType = this.__TypeHandle.__Game_Buildings_AttractivenessProvider_RO_ComponentTypeHandle,
                m_City = this.m_CitySystem.City,
                m_SeasonCloudiness = this.m_ClimateSystem.seasonCloudiness,
                m_SeasonRain = this.m_ClimateSystem.seasonPrecipitation,
                m_SeasonTemperature = this.m_ClimateSystem.seasonTemperature,
                m_daytype = this.m_daytype
            };
            this.Dependency = jobData.Schedule<Time2WorkTourismSystem.TourismJob>(JobHandle.CombineDependencies(deps, this.Dependency));

            this.m_CityStatisticsSystem.AddWriter(this.Dependency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            this.__AssignQueries(ref this.CheckedStateRef);
            this.__TypeHandle.__AssignHandles(ref this.CheckedStateRef);
        }

        [UnityEngine.Scripting.Preserve]
        public Time2WorkTourismSystem()
        {
        }

        [BurstCompile]
        private struct TourismJob : IJob
        {
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<ArchetypeChunk> m_TouristHouseholdChunks;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<ArchetypeChunk> m_HotelChunks;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<ArchetypeChunk> m_Chunks;
            [ReadOnly]
            public ComponentTypeHandle<AttractivenessProvider> m_ProviderType;
            [ReadOnly]
            public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;
            [ReadOnly]
            public ComponentTypeHandle<LodgingProvider> m_LodgingProviderType;
            [ReadOnly]
            public BufferTypeHandle<Renter> m_RenterType;
            [ReadOnly]
            public AttractivenessParameterData m_Parameters;
            public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;
            [ReadOnly]
            public BufferLookup<CityModifier> m_CityModifiers;
            public ComponentLookup<Tourism> m_Tourisms;
            public Entity m_City;
            public float m_SeasonTemperature;
            public float m_SeasonRain;
            public float m_SeasonCloudiness;
            public Setting.DTSimulationEnum m_daytype;

            public void Execute()
            {
                Tourism tourism = new Tourism();
                int num1 = 0;
                for (int index1 = 0; index1 < this.m_TouristHouseholdChunks.Length; ++index1)
                {
                    ArchetypeChunk touristHouseholdChunk = this.m_TouristHouseholdChunks[index1];
                    BufferAccessor<HouseholdCitizen> bufferAccessor = touristHouseholdChunk.GetBufferAccessor<HouseholdCitizen>(ref this.m_HouseholdCitizenType);
                    for (int index2 = 0; index2 < touristHouseholdChunk.Count; ++index2)
                    {
                        DynamicBuffer<HouseholdCitizen> dynamicBuffer = bufferAccessor[index2];
                        num1 += dynamicBuffer.Length;
                    }
                }

                this.m_StatisticsEventQueue.Enqueue(new StatisticsEvent()
                {
                    m_Statistic = StatisticType.TouristCount,
                    m_Change = (float)num1
                });
                tourism.m_CurrentTourists = num1;
                int2 int2 = new int2();

                for (int index3 = 0; index3 < this.m_HotelChunks.Length; ++index3)
                {
                    ArchetypeChunk hotelChunk = this.m_HotelChunks[index3];
                    NativeArray<LodgingProvider> nativeArray = hotelChunk.GetNativeArray<LodgingProvider>(ref this.m_LodgingProviderType);
                    BufferAccessor<Renter> bufferAccessor = hotelChunk.GetBufferAccessor<Renter>(ref this.m_RenterType);
                    for (int index4 = 0; index4 < hotelChunk.Count; ++index4)
                    {
                        LodgingProvider lodgingProvider = nativeArray[index4];
                        DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[index4];
                        int2 += new int2(dynamicBuffer.Length, dynamicBuffer.Length + lodgingProvider.m_FreeRooms);
                    }
                }
                tourism.m_Lodging = int2;
                float num2 = 0.0f;
                for (int index5 = 0; index5 < this.m_Chunks.Length; ++index5)
                {
                    NativeArray<AttractivenessProvider> nativeArray = this.m_Chunks[index5].GetNativeArray<AttractivenessProvider>(ref this.m_ProviderType);
                    for (int index6 = 0; index6 < nativeArray.Length; ++index6)
                    {
                        AttractivenessProvider attractivenessProvider = nativeArray[index6];
                        num2 += (float)(attractivenessProvider.m_Attractiveness * attractivenessProvider.m_Attractiveness) / 10000f;
                    }
                }
                float f = (float)(200.0 / (1.0 + (double)math.exp(-0.3f * num2)) - 100.0);
                DynamicBuffer<CityModifier> cityModifier = this.m_CityModifiers[this.m_City];
                CityUtils.ApplyModifier(ref f, cityModifier, CityModifierType.Attractiveness);
                tourism.m_Attractiveness = Mathf.RoundToInt(f);
                tourism.m_AverageTourists = Mathf.RoundToInt((float)(2.0 * (double)Time2WorkTourismSystem.GetTouristProbability(this.m_Parameters, tourism.m_Attractiveness, this.m_SeasonTemperature, this.m_SeasonRain, this.m_SeasonCloudiness, this.m_daytype) * 100000.0 / 16.0));
                this.m_Tourisms[this.m_City] = tourism;
            }
        }

        private struct TypeHandle
        {
            [ReadOnly]
            public BufferTypeHandle<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<LodgingProvider> __Game_Companies_LodgingProvider_RO_ComponentTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;
            [ReadOnly]
            public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;
            public ComponentLookup<Tourism> __Game_City_Tourism_RW_ComponentLookup;
            [ReadOnly]
            public ComponentTypeHandle<AttractivenessProvider> __Game_Buildings_AttractivenessProvider_RO_ComponentTypeHandle;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                this.__Game_Citizens_HouseholdCitizen_RO_BufferTypeHandle = state.GetBufferTypeHandle<HouseholdCitizen>(true);
                this.__Game_Companies_LodgingProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LodgingProvider>(true);
                this.__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(true);
                this.__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(true);
                this.__Game_City_Tourism_RW_ComponentLookup = state.GetComponentLookup<Tourism>();
                this.__Game_Buildings_AttractivenessProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AttractivenessProvider>(true);
            }
        }
    }
}
