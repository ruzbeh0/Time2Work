
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
        private CountHouseholdDataSystem m_CountHouseholdDataSystem;
        private EntityQuery m_AttractivenessProviderGroup;
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
          ClimateSystem.WeatherClassification weatherClassification,
          float temperature,
          float precipitation,
          bool isRaining,
          bool isSnowing,
          Setting.DTSimulationEnum dayType)
        {
            float prob = Time2WorkTourismSystem.GetRawTouristProbability(attractiveness) * Time2WorkTourismSystem.GetWeatherEffect(parameterData, weatherClassification, temperature, precipitation, isRaining, isSnowing);
            
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
            ClimateSystem.WeatherClassification weatherClassification,
            float temperature,
            float precipitation,
            bool isRaining,
            bool isSnowing)
        {
            float x = 1f;
            if ((double)temperature > (double)parameterData.m_AttractiveTemperature.x && (double)temperature < (double)parameterData.m_AttractiveTemperature.y)
                x += Mathf.Lerp(parameterData.m_TemperatureAffect.x, 0.0f, math.abs(temperature - (float)(((double)parameterData.m_AttractiveTemperature.x + (double)parameterData.m_AttractiveTemperature.y) / 2.0)) / (float)(((double)parameterData.m_AttractiveTemperature.y - (double)parameterData.m_AttractiveTemperature.x) / 2.0));
            else if ((double)temperature > (double)parameterData.m_ExtremeTemperature.y)
                x += Mathf.Lerp(0.0f, parameterData.m_TemperatureAffect.y, (float)(((double)temperature - (double)parameterData.m_ExtremeTemperature.y) / 10.0));
            else if ((double)temperature < (double)parameterData.m_ExtremeTemperature.x)
                x += Mathf.Lerp(0.0f, parameterData.m_TemperatureAffect.y, (float)(((double)parameterData.m_ExtremeTemperature.x - (double)temperature) / 10.0));
            if (isSnowing && (double)precipitation > (double)parameterData.m_SnowEffectRange.x && (double)precipitation < (double)parameterData.m_SnowEffectRange.y)
                x += Mathf.Lerp(0.0f, parameterData.m_SnowRainExtremeAffect.x, (float)(((double)precipitation - (double)parameterData.m_SnowEffectRange.x) / ((double)parameterData.m_SnowEffectRange.y - (double)parameterData.m_SnowEffectRange.x)));
            else if (isRaining && (double)precipitation > (double)parameterData.m_RainEffectRange.x && (double)precipitation < (double)parameterData.m_RainEffectRange.y)
                x += Mathf.Lerp(0.0f, parameterData.m_SnowRainExtremeAffect.y, (float)(((double)precipitation - (double)parameterData.m_RainEffectRange.x) / ((double)parameterData.m_RainEffectRange.y - (double)parameterData.m_RainEffectRange.x)));
            if (weatherClassification == ClimateSystem.WeatherClassification.Stormy)
                x += parameterData.m_SnowRainExtremeAffect.z;
            return math.clamp(x, 0.5f, 1.5f);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_CitySystem = this.World.GetOrCreateSystemManaged<CitySystem>();
            this.m_ClimateSystem = this.World.GetOrCreateSystemManaged<ClimateSystem>();
            this.m_CountHouseholdDataSystem = this.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
            this.m_AttractivenessProviderGroup = this.GetEntityQuery(ComponentType.ReadWrite<AttractivenessProvider>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
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
                m_m_AttractivenessProviderChunks = this.m_AttractivenessProviderGroup.ToArchetypeChunkArray((AllocatorManager.AllocatorHandle)Allocator.TempJob),
                m_HotelChunks = this.m_LodgingGroup.ToArchetypeChunkArray((AllocatorManager.AllocatorHandle)Allocator.TempJob),
                m_LodgingProviderType = this.__TypeHandle.__Game_Companies_LodgingProvider_RO_ComponentTypeHandle,
                m_RenterType = this.__TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle,
                m_CityModifiers = this.__TypeHandle.__Game_City_CityModifier_RO_BufferLookup,
                m_Tourisms = this.__TypeHandle.__Game_City_Tourism_RW_ComponentLookup,
                m_Parameters = this.m_ParameterQuery.GetSingleton<AttractivenessParameterData>(),
                m_ProviderType = this.__TypeHandle.__Game_Buildings_AttractivenessProvider_RO_ComponentTypeHandle,
                m_City = this.m_CitySystem.City,
                m_IsRaining = this.m_ClimateSystem.isRaining,
                m_IsSnowing = this.m_ClimateSystem.isSnowing,
                m_Temperature = (float)this.m_ClimateSystem.temperature,
                m_Precipitation = (float)this.m_ClimateSystem.precipitation,
                m_TouristCitizenCount = this.m_CountHouseholdDataSystem.TouristCitizenCount,
                m_WeatherClassification = this.m_ClimateSystem.classification,
                m_daytype = this.m_daytype
            };
            this.Dependency = jobData.Schedule<Time2WorkTourismSystem.TourismJob>(this.Dependency);
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
            public NativeArray<ArchetypeChunk> m_HotelChunks;
            [DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<ArchetypeChunk> m_m_AttractivenessProviderChunks;
            [ReadOnly]
            public ComponentTypeHandle<AttractivenessProvider> m_ProviderType;
            [ReadOnly]
            public ComponentTypeHandle<LodgingProvider> m_LodgingProviderType;
            [ReadOnly]
            public BufferTypeHandle<Renter> m_RenterType;
            [ReadOnly]
            public AttractivenessParameterData m_Parameters;
            [ReadOnly]
            public BufferLookup<CityModifier> m_CityModifiers;
            public ComponentLookup<Tourism> m_Tourisms;
            [ReadOnly]
            public Entity m_City;
            [ReadOnly]
            public bool m_IsRaining;
            [ReadOnly]
            public bool m_IsSnowing;
            [ReadOnly]
            public float m_Temperature;
            [ReadOnly]
            public float m_Precipitation;
            [ReadOnly]
            public int m_TouristCitizenCount;
            [ReadOnly]
            public ClimateSystem.WeatherClassification m_WeatherClassification;

            public Setting.DTSimulationEnum m_daytype;

            public void Execute()
            {
                Tourism tourism = new Tourism();
                tourism.m_CurrentTourists = this.m_TouristCitizenCount;
                int2 int2 = new int2();

                for (int index1 = 0; index1 < this.m_HotelChunks.Length; ++index1)
                {
                    ArchetypeChunk hotelChunk = this.m_HotelChunks[index1];
                    NativeArray<LodgingProvider> nativeArray = hotelChunk.GetNativeArray<LodgingProvider>(ref this.m_LodgingProviderType);
                    BufferAccessor<Renter> bufferAccessor = hotelChunk.GetBufferAccessor<Renter>(ref this.m_RenterType);
                    for (int index2 = 0; index2 < hotelChunk.Count; ++index2)
                    {
                        LodgingProvider lodgingProvider = nativeArray[index2];
                        DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[index2];
                        int2 += new int2(dynamicBuffer.Length, dynamicBuffer.Length + lodgingProvider.m_FreeRooms);
                    }
                }
                tourism.m_Lodging = int2;
                float num = 0.0f;
                for (int index3 = 0; index3 < this.m_m_AttractivenessProviderChunks.Length; ++index3)
                {
                    NativeArray<AttractivenessProvider> nativeArray = this.m_m_AttractivenessProviderChunks[index3].GetNativeArray<AttractivenessProvider>(ref this.m_ProviderType);
                    for (int index4 = 0; index4 < nativeArray.Length; ++index4)
                    {
                        AttractivenessProvider attractivenessProvider = nativeArray[index4];
                        num += (float)(attractivenessProvider.m_Attractiveness * attractivenessProvider.m_Attractiveness) / 10000f;
                    }
                }
                float f = (float)(200.0 / (1.0 + (double)math.exp(-0.3f * num)) - 100.0);
                DynamicBuffer<CityModifier> cityModifier = this.m_CityModifiers[this.m_City];
                CityUtils.ApplyModifier(ref f, cityModifier, CityModifierType.Attractiveness);
                tourism.m_Attractiveness = Mathf.RoundToInt(f);
                tourism.m_AverageTourists = Mathf.RoundToInt((float)(2.0 * (double)Time2WorkTourismSystem.GetTouristProbability(this.m_Parameters, tourism.m_Attractiveness, this.m_WeatherClassification, this.m_Temperature, this.m_Precipitation, this.m_IsRaining, this.m_IsSnowing, this.m_daytype) * 100000.0 / 16.0));
                this.m_Tourisms[this.m_City] = tourism;
            }
        }

        private struct TypeHandle
        {
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
                this.__Game_Companies_LodgingProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LodgingProvider>(true);
                this.__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(true);
                this.__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(true);
                this.__Game_City_Tourism_RW_ComponentLookup = state.GetComponentLookup<Tourism>();
                this.__Game_Buildings_AttractivenessProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AttractivenessProvider>(true);
            }
        }
    }
}
