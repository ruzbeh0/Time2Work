
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.UI;
using Game.UI.InGame;
using Game.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using static Unity.Collections.AllocatorManager;

#nullable disable
namespace Time2Work.Systems
{
    //[CompilerGenerated]
    public partial class Time2WorkStatisticsUISystem : UISystemBase
    {
        private const string kGroup = "statistics";
        private PrefabUISystem m_PrefabUISystem;
        private PrefabSystem m_PrefabSystem;
        private ResourceSystem m_ResourceSystem;
        private ICityStatisticsSystem m_CityStatisticsSystem;
        private CityConfigurationSystem m_CityConfigurationSystem;
        private GameModeGovernmentSubsidiesSystem m_GameModeGovernmentSubsidiesSystem;
        private Time2WorkTimeUISystem m_TimeUISystem;
        private EntityQuery m_StatisticsCategoryQuery;
        private EntityQuery m_TimeDataQuery;
        private EntityQuery m_UnlockedPrefabQuery;
        private EntityQuery m_LinePrefabQuery;
        private List<Time2WorkStatisticsUISystem.StatItem> m_GroupCache;
        private List<Time2WorkStatisticsUISystem.StatItem> m_SubGroupCache;
        private List<Time2WorkStatisticsUISystem.StatItem> m_SelectedStatistics;
        private List<Time2WorkStatisticsUISystem.StatItem> m_SelectedStatisticsTracker;
        private Entity m_ActiveCategory;
        private Entity m_ActiveGroup;
        private int m_SampleRange;
        private bool m_Stacked;
        private RawMapBinding<Entity> m_GroupsMapBinding;
        private ValueBinding<int> m_SampleRangeBinding;
        private ValueBinding<int> m_SampleCountBinding;
        private GetterValueBinding<Entity> m_ActiveGroupBinding;
        private GetterValueBinding<Entity> m_ActiveCategoryBinding;
        private GetterValueBinding<bool> m_StackedBinding;
        private RawValueBinding m_SelectedStatisticsBinding;
        private RawValueBinding m_CategoriesBinding;
        private RawValueBinding m_DataBinding;
        private RawMapBinding<Entity> m_UnlockingRequirementsBinding;
        private bool m_ClearActive = true;
        private int m_UnlockRequirementVersion;
        private MapTilePurchaseSystem m_MapTilePurchaseSystem; 


        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            this.m_MapTilePurchaseSystem = this.World.GetOrCreateSystemManaged<MapTilePurchaseSystem>(); 
            this.m_StatisticsCategoryQuery = this.GetEntityQuery(ComponentType.ReadOnly<UIObjectData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<UIStatisticsCategoryData>());
            this.m_TimeDataQuery = this.GetEntityQuery(ComponentType.ReadOnly<TimeData>());
            this.m_UnlockedPrefabQuery = this.GetEntityQuery(ComponentType.ReadOnly<Unlock>());
            this.m_LinePrefabQuery = this.GetEntityQuery(ComponentType.ReadOnly<TransportLineData>());
            this.m_GroupCache = new List<Time2WorkStatisticsUISystem.StatItem>();
            this.m_SubGroupCache = new List<Time2WorkStatisticsUISystem.StatItem>();
            this.m_SelectedStatistics = new List<Time2WorkStatisticsUISystem.StatItem>();
            this.m_SelectedStatisticsTracker = new List<Time2WorkStatisticsUISystem.StatItem>();
            this.m_PrefabUISystem = this.World.GetOrCreateSystemManaged<PrefabUISystem>();
            this.m_PrefabSystem = this.World.GetOrCreateSystemManaged<PrefabSystem>();
            this.m_ResourceSystem = this.World.GetOrCreateSystemManaged<ResourceSystem>();
            this.m_CityStatisticsSystem = (ICityStatisticsSystem)this.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            this.m_CityStatisticsSystem.eventStatisticsUpdated += (System.Action)(() => this.m_DataBinding.Update());
            this.m_CityConfigurationSystem = this.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            this.m_TimeUISystem = this.World.GetOrCreateSystemManaged<Time2WorkTimeUISystem>();
            this.AddBinding((IBinding)(this.m_GroupsMapBinding = new RawMapBinding<Entity>("statistics", "groups", (Action<IJsonWriter, Entity>)((binder, parent) =>
            {
                this.CacheChildren(parent, this.m_GroupCache);
                binder.ArrayBegin(this.m_GroupCache.Count);
                for (int index = 0; index < this.m_GroupCache.Count; ++index)
                {
                    binder.Write<Time2WorkStatisticsUISystem.StatItem>(this.m_GroupCache[index]);
                }
                binder.ArrayEnd();
            }))));
            this.AddBinding((IBinding)(this.m_SampleRangeBinding = new ValueBinding<int>("statistics", "sampleRange", this.m_SampleRange)));
            this.AddBinding((IBinding)(this.m_SampleCountBinding = new ValueBinding<int>("statistics", "sampleCount", this.m_CityStatisticsSystem.sampleCount)));
            this.AddBinding((IBinding)(this.m_ActiveGroupBinding = new GetterValueBinding<Entity>("statistics", "activeGroup", (Func<Entity>)(() => this.m_ActiveGroup))));
            this.AddBinding((IBinding)(this.m_ActiveCategoryBinding = new GetterValueBinding<Entity>("statistics", "activeCategory", (Func<Entity>)(() => this.m_ActiveCategory))));
            this.AddBinding((IBinding)(this.m_StackedBinding = new GetterValueBinding<bool>("statistics", "stacked", (Func<bool>)(() => this.m_Stacked))));
            this.AddBinding((IBinding)(this.m_CategoriesBinding = new RawValueBinding("statistics", "categories", (Action<IJsonWriter>)(binder =>
            {
                NativeList<Time2WorkStatisticsUISystem.StatCategory> sortedCategories = this.GetSortedCategories();
                binder.ArrayBegin(sortedCategories.Length);
                for (int index = 0; index < sortedCategories.Length; ++index)
                {
                    Time2WorkStatisticsUISystem.StatCategory statCategory = sortedCategories[index];
                    PrefabBase prefab = this.m_PrefabSystem.GetPrefab<PrefabBase>(statCategory.m_PrefabData);
                    bool flag = this.EntityManager.HasEnabledComponent<Locked>(statCategory.m_Entity);
                    binder.TypeBegin("statistics.StatCategory");
                    binder.PropertyName("entity");
                    binder.Write(statCategory.m_Entity);
                    binder.PropertyName("key");
                    binder.Write(prefab.name);
                    binder.PropertyName("locked");
                    binder.Write(flag);
                    binder.TypeEnd();
                }
                binder.ArrayEnd();
            }))));

            this.AddBinding((IBinding)(this.m_DataBinding = new RawValueBinding("statistics", "data", (Action<IJsonWriter>)(binder =>
            {
                binder.ArrayBegin(this.m_SelectedStatistics.Count);
                for (int index = this.m_SelectedStatistics.Count - 1; index >= 0; --index)
                {
                    this.BindData(binder, this.m_SelectedStatistics[index]);
                }
                binder.ArrayEnd();
            }))));
            this.AddBinding((IBinding)(this.m_SelectedStatisticsBinding = new RawValueBinding("statistics", "selectedStatistics", (Action<IJsonWriter>)(binder =>
            {
                binder.ArrayBegin(this.m_SelectedStatistics.Count);
                for (int index = 0; index < this.m_SelectedStatistics.Count; ++index)
                {
                    Time2WorkStatisticsUISystem.StatItem selectedStatistic = this.m_SelectedStatistics[index];
                    binder.Write<Time2WorkStatisticsUISystem.StatItem>(selectedStatistic);
                }
                binder.ArrayEnd();
            }))));
            this.AddBinding((IBinding)(this.m_UnlockingRequirementsBinding = new RawMapBinding<Entity>("statistics", "unlockingRequirements", (Action<IJsonWriter, Entity>)((writer, prefabEntity) => this.m_PrefabUISystem.BindPrefabRequirements(writer, prefabEntity)))));
            this.AddBinding((IBinding)new GetterValueBinding<int>("statistics", "updatesPerDay", (Func<int>)(() => 32)));
            this.AddBinding((IBinding)new TriggerBinding<Time2WorkStatisticsUISystem.StatItem>("statistics", "addStat", (Action<Time2WorkStatisticsUISystem.StatItem>)(stat =>
            {
                if (stat.locked)
                    return;
                if (stat.category != this.m_ActiveCategory)
                {
                    this.m_SelectedStatistics.Clear();
                    this.m_SelectedStatisticsTracker.Clear();
                    this.m_ActiveCategory = stat.category;
                    this.m_ActiveCategoryBinding.Update();
                }
                if (this.m_ActiveGroup == Entity.Null || stat.isGroup || stat.group != this.m_ActiveGroup)
                {
                    this.m_SelectedStatistics.Clear();
                    this.m_SelectedStatisticsTracker.Clear();
                    this.m_ActiveGroup = stat.isGroup ? stat.entity : stat.group;
                    this.m_ActiveGroupBinding.Update();
                }
                if (stat.isSubgroup)
                {
                    int num = this.m_SelectedStatisticsTracker.Count<Time2WorkStatisticsUISystem.StatItem>((Func<Time2WorkStatisticsUISystem.StatItem, bool>)(s => s.isSubgroup));
                    if (num == 1)
                    {
                        Time2WorkStatisticsUISystem.StatItem stat1 = this.m_SelectedStatisticsTracker.First<Time2WorkStatisticsUISystem.StatItem>((Func<Time2WorkStatisticsUISystem.StatItem, bool>)(s => s.isSubgroup));
                        this.m_ClearActive = false;
                        this.DeepRemoveStat(stat1);
                        this.AddStat(stat1);
                    }
                    if (num == 0)
                    {
                        this.m_SelectedStatisticsTracker.Add(stat);
                        this.TryAddChildren(stat, this.m_SubGroupCache);
                    }
                    else
                    {
                        this.AddStat(stat);
                    }
                }
                else
                {
                    if (stat.isGroup)
                    {
                        this.m_SelectedStatisticsTracker.Add(stat);
                        if (!this.TryAddChildren(stat, this.m_GroupCache))
                        {
                            this.m_SelectedStatistics.Add(stat);
                        }
                    }
                    else
                    {
                        this.AddStat(stat);
                    }
                }
                this.UpdateStackedStatus();
                this.UpdateStats();
            }), (IReader<Time2WorkStatisticsUISystem.StatItem>)new ValueReader<Time2WorkStatisticsUISystem.StatItem>()));
            // NEW: allow adding all children of a subgroup in one click
            this.AddBinding(new TriggerBinding<Time2WorkStatisticsUISystem.StatItem>(
                "statistics",
                "addStatChildren",
                (stat) =>
                {
                    if (stat.locked)
                        return;

                    // keep category/group in sync with the clicked stat
                    if (stat.category != m_ActiveCategory)
                    {
                        m_SelectedStatistics.Clear();
                        m_SelectedStatisticsTracker.Clear();
                        m_ActiveCategory = stat.category;
                        m_ActiveCategoryBinding.Update();
                    }

                    if (m_ActiveGroup == Entity.Null || stat.isGroup || stat.group != m_ActiveGroup)
                    {
                        m_SelectedStatistics.Clear();
                        m_SelectedStatisticsTracker.Clear();
                        m_ActiveGroup = stat.isGroup ? stat.entity : stat.group;
                        m_ActiveGroupBinding.Update();
                    }

                    // only meaningful for subgroups
                    if (!stat.isSubgroup)
                        return;

                    // remove the subgroup itself (if present) and add its children
                    RemoveStat(stat);
                    TryAddChildren(stat, m_SubGroupCache);

                    UpdateStackedStatus();
                    UpdateStats();
                }
            ));


            this.AddBinding((IBinding)new TriggerBinding<Time2WorkStatisticsUISystem.StatItem>("statistics", "removeStat", (Action<Time2WorkStatisticsUISystem.StatItem>)(stat =>
            {
                if (!this.m_SelectedStatisticsTracker.Contains(stat))
                {
                    int index1 = this.m_SelectedStatisticsTracker.FindIndex((Predicate<Time2WorkStatisticsUISystem.StatItem>)(s => s.entity == stat.group));
                    int index2 = this.m_SelectedStatisticsTracker.FindIndex((Predicate<Time2WorkStatisticsUISystem.StatItem>)(s => s.entity == stat.entity && s.isSubgroup));
                    if (index1 >= 0)
                    {
                        Time2WorkStatisticsUISystem.StatItem stat2 = this.m_SelectedStatisticsTracker[index1];
                        if (index2 >= 0)
                        {
                            Time2WorkStatisticsUISystem.StatItem stat3 = this.m_SelectedStatisticsTracker[index2];
                            this.DeepRemoveStat(stat2);
                            this.ProcessAddStat(stat3);
                        }
                        else
                        {
                            this.DeepRemoveStat(stat2);
                        }
                    }
                }
                int num1 = this.m_SelectedStatisticsTracker.Count<Time2WorkStatisticsUISystem.StatItem>((Func<Time2WorkStatisticsUISystem.StatItem, bool>)(s => s.isSubgroup));
                this.RemoveStat(stat);
                if (stat.isGroup)
                {
                    for (int index = this.m_SelectedStatistics.Count - 1; index >= 0; --index)
                    {
                        if (this.m_SelectedStatistics[index].group == stat.entity)
                        {
                            this.m_SelectedStatistics.RemoveAt(index);
                        }
                    }
                    for (int index = this.m_SelectedStatisticsTracker.Count - 1; index >= 0; --index)
                    {
                        if (this.m_SelectedStatisticsTracker[index].group == stat.entity)
                        {
                            this.m_SelectedStatisticsTracker.RemoveAt(index);
                        }
                    }
                }
                if (stat.isSubgroup)
                {
                    for (int index = this.m_SelectedStatistics.Count - 1; index >= 0; --index)
                    {
                        if (this.m_SelectedStatistics[index].entity == stat.entity)
                        {
                            this.m_SelectedStatistics.RemoveAt(index);
                        }
                    }
                    for (int index = this.m_SelectedStatisticsTracker.Count - 1; index >= 0; --index)
                    {
                        if (this.m_SelectedStatisticsTracker[index].entity == stat.entity)
                        {
                            this.m_SelectedStatisticsTracker.RemoveAt(index);
                        }
                    }
                }
                int num2 = this.m_SelectedStatisticsTracker.Count<Time2WorkStatisticsUISystem.StatItem>((Func<Time2WorkStatisticsUISystem.StatItem, bool>)(s => s.isSubgroup));
                if (num1 > 1 && num2 == 1)
                {
                    Time2WorkStatisticsUISystem.StatItem stat4 = this.m_SelectedStatisticsTracker.First<Time2WorkStatisticsUISystem.StatItem>((Func<Time2WorkStatisticsUISystem.StatItem, bool>)(s => s.isSubgroup));
                    this.RemoveStat(stat4);
                    this.ProcessAddStat(stat4);
                }
                if (this.m_ClearActive && this.m_SelectedStatistics.Count == 0 && this.m_SelectedStatisticsTracker.Count <= 1)
                {
                    this.ClearStats();
                }
                else
                {
                    this.UpdateStats();
                }
                this.m_ClearActive = true;
                this.UpdateStackedStatus();
            }), (IReader<Time2WorkStatisticsUISystem.StatItem>)new ValueReader<Time2WorkStatisticsUISystem.StatItem>()));
            this.AddBinding((IBinding)new TriggerBinding("statistics", "clearStats", (System.Action)(() =>
            {
                this.m_SelectedStatistics.Clear();
                this.m_SelectedStatisticsTracker.Clear();
                this.UpdateStats();
                this.ClearActive();
            })));
            this.AddBinding((IBinding)new TriggerBinding<int>("statistics", "setSampleRange", (Action<int>)(range =>
            {
                this.m_SampleRange = range;
                this.m_SampleRangeBinding.Update(this.m_SampleRange);
                this.UpdateStats();
            })));
        }

        private void BindUnlockingRequirements(IJsonWriter writer, Entity prefabEntity)
        {
            this.m_PrefabUISystem.BindPrefabRequirements(writer, prefabEntity);
        }

        protected override void OnGameLoaded(Colossal.Serialization.Entities.Context serializationContext)
        {
            this.m_SelectedStatistics.Clear();
            this.m_SampleRange = 32;
            //this.m_SampleRange = (int)(this.m_SampleRange * Mod.m_Setting.slow_time_factor) - 1;
        }

        [Preserve]
        protected override void OnUpdate()
        {
            this.m_SampleCountBinding.Update(this.m_CityStatisticsSystem.sampleCount);
            this.m_SampleRangeBinding.Update(this.m_SampleRange);
            int componentOrderVersion = this.EntityManager.GetComponentOrderVersion<UnlockRequirementData>();
            if (PrefabUtils.HasUnlockedPrefab<UIObjectData>(this.EntityManager, this.m_UnlockedPrefabQuery) || this.m_UnlockRequirementVersion != componentOrderVersion)
            {
                this.m_UnlockingRequirementsBinding.UpdateAll();
                this.m_GroupsMapBinding.UpdateAll();
                this.m_CategoriesBinding.Update();
            }
            this.m_UnlockRequirementVersion = componentOrderVersion;
        }

        [Preserve]
        protected override void OnDestroy()
        {
            this.m_CityStatisticsSystem.eventStatisticsUpdated -= (System.Action)(() => this.m_DataBinding.Update());
            base.OnDestroy();
        }

        private void OnStatisticsUpdated() => this.m_DataBinding.Update();

        private void BindSelectedStatistics(IJsonWriter binder)
        {
            binder.ArrayBegin(this.m_SelectedStatistics.Count);
            for (int index = 0; index < this.m_SelectedStatistics.Count; ++index)
            {
                Time2WorkStatisticsUISystem.StatItem selectedStatistic = this.m_SelectedStatistics[index];
                binder.Write<Time2WorkStatisticsUISystem.StatItem>(selectedStatistic);
            }
            binder.ArrayEnd();
        }

        private void BindCategories(IJsonWriter binder)
        {
            NativeList<Time2WorkStatisticsUISystem.StatCategory> sortedCategories = this.GetSortedCategories();
            binder.ArrayBegin(sortedCategories.Length);
            for (int index = 0; index < sortedCategories.Length; ++index)
            {
                Time2WorkStatisticsUISystem.StatCategory statCategory = sortedCategories[index];
                PrefabBase prefab = this.m_PrefabSystem.GetPrefab<PrefabBase>(statCategory.m_PrefabData);
                bool flag = this.EntityManager.HasEnabledComponent<Locked>(statCategory.m_Entity);
                binder.TypeBegin("statistics.StatCategory");
                binder.PropertyName("entity");
                binder.Write(statCategory.m_Entity);
                binder.PropertyName("key");
                binder.Write(prefab.name);
                binder.PropertyName("locked");
                binder.Write(flag);
                binder.TypeEnd();
            }
            binder.ArrayEnd();
        }

        private NativeList<Time2WorkStatisticsUISystem.StatCategory> GetSortedCategories()
        {
            NativeArray<Entity> entityArray = this.m_StatisticsCategoryQuery.ToEntityArray((AllocatorManager.AllocatorHandle)Allocator.TempJob);
            NativeArray<UIObjectData> componentDataArray1 = this.m_StatisticsCategoryQuery.ToComponentDataArray<UIObjectData>((AllocatorManager.AllocatorHandle)Allocator.TempJob);
            NativeArray<PrefabData> componentDataArray2 = this.m_StatisticsCategoryQuery.ToComponentDataArray<PrefabData>((AllocatorManager.AllocatorHandle)Allocator.TempJob);
            NativeList<Time2WorkStatisticsUISystem.StatCategory> list = new NativeList<Time2WorkStatisticsUISystem.StatCategory>(entityArray.Length, (AllocatorManager.AllocatorHandle)Allocator.Temp);
            for (int index = 0; index < entityArray.Length; ++index)
            {
                ref NativeList<Time2WorkStatisticsUISystem.StatCategory> local1 = ref list;
                Time2WorkStatisticsUISystem.StatCategory statCategory = new Time2WorkStatisticsUISystem.StatCategory(entityArray[index], componentDataArray1[index], componentDataArray2[index]);
                ref Time2WorkStatisticsUISystem.StatCategory local2 = ref statCategory;
                local1.Add(in local2);
            }
            entityArray.Dispose();
            componentDataArray1.Dispose();
            componentDataArray2.Dispose();
            list.Sort<Time2WorkStatisticsUISystem.StatCategory>();
            return list;
        }

        private void CacheChildren(Entity parentEntity, List<Time2WorkStatisticsUISystem.StatItem> cache)
        {
            cache.Clear();
            bool isGroup = this.EntityManager.HasComponent<UIStatisticsCategoryData>(parentEntity);
            DynamicBuffer<UIGroupElement> buffer;
            if (this.EntityManager.TryGetBuffer<UIGroupElement>(parentEntity, true, out buffer))
            {
                NativeList<UIObjectInfo> sortedObjects = UIObjectInfo.GetSortedObjects(this.EntityManager, buffer, Allocator.TempJob);
                for (int index = 0; index < sortedObjects.Length; ++index)
                {
                    Entity category = Entity.Null;
                    Entity group = Entity.Null;
                    Entity entity = sortedObjects[index].entity;
                    PrefabBase prefab = this.m_PrefabSystem.GetPrefab<PrefabBase>(entity);

                    if (prefab.name == "MapTileUpkeep" && !m_MapTilePurchaseSystem.GetMapTileUpkeepEnabled())
                        continue;

                    StatisticUnitType unitType = StatisticUnitType.None;
                    StatisticType statisticType = StatisticType.Invalid;
                    bool locked = this.EntityManager.HasEnabledComponent<Locked>(entity);
                    bool isSubgroup = !isGroup && this.EntityManager.HasComponent<UIStatisticsGroupData>(entity) || prefab is ParametricStatistic parametricStatistic && parametricStatistic.GetParameters().Count<StatisticParameterData>() > 1;
                    bool stacked = true;
                    Color color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                    StatisticsData component1;
                    if (this.EntityManager.TryGetComponent<StatisticsData>(entity, out component1))
                    {
                        if (!this.m_CityConfigurationSystem.unlimitedMoney || component1.m_StatisticType != StatisticType.Money)
                        {
                            unitType = component1.m_UnitType;
                            statisticType = component1.m_StatisticType;
                            group = component1.m_Group;
                            category = component1.m_Category;
                            color = component1.m_Color;
                            stacked = component1.m_Stacked;
                        }
                        else
                            continue;
                    }
                    UIStatisticsGroupData component2;
                    UIObjectData component3;
                    if (this.EntityManager.TryGetComponent<UIStatisticsGroupData>(entity, out component2) && this.EntityManager.TryGetComponent<UIObjectData>(entity, out component3))
                    {
                        group = component3.m_Group == component2.m_Category ? entity : component3.m_Group;
                        unitType = component2.m_UnitType;
                        category = component2.m_Category;
                        color = component2.m_Color;
                        stacked = component2.m_Stacked;
                    }
                    cache.Add(new Time2WorkStatisticsUISystem.StatItem(index, category, group, entity, (int)statisticType, unitType, 0, prefab.name, color, locked, isGroup, isSubgroup, stacked));
                }
                sortedObjects.Dispose();
            }
            else
            {
                StatisticsData component4;
                PrefabData component5;
                if (!this.EntityManager.TryGetComponent<StatisticsData>(parentEntity, out component4) || !this.EntityManager.TryGetComponent<PrefabData>(parentEntity, out component5))
                    return;
                bool locked = this.EntityManager.HasEnabledComponent<Locked>(parentEntity);
                this.CacheParameterChildren(parentEntity, locked, component4, component5, cache);
            }
        }

        private void CacheParameterChildren(
          Entity parent,
          bool locked,
          StatisticsData statisticsData,
          PrefabData prefabData,
          List<Time2WorkStatisticsUISystem.StatItem> cache)
        {
            ParametricStatistic prefab = this.m_PrefabSystem.GetPrefab<ParametricStatistic>(prefabData);
            DynamicBuffer<StatisticParameterData> buffer;
            if (!this.EntityManager.TryGetBuffer<StatisticParameterData>(parent, true, out buffer))
                return;
            for (int index = 0; index < buffer.Length; ++index)
            {
                cache.Add(new Time2WorkStatisticsUISystem.StatItem(index, statisticsData.m_Category, statisticsData.m_Group == Entity.Null ? parent : statisticsData.m_Group, parent, (int)prefab.m_StatisticsType, prefab.m_UnitType, index, prefab.name + prefab.GetParameterName(buffer[index].m_Value), buffer[index].m_Color, locked, stacked: statisticsData.m_Stacked));
            }
        }

        private void BindGroups(IJsonWriter binder, Entity parent)
        {
            this.CacheChildren(parent, this.m_GroupCache);
            binder.ArrayBegin(this.m_GroupCache.Count);
            for (int index = 0; index < this.m_GroupCache.Count; ++index)
            {
                binder.Write<Time2WorkStatisticsUISystem.StatItem>(this.m_GroupCache[index]);
            }
            binder.ArrayEnd();
        }

        private void BindData(IJsonWriter binder)
        {
            binder.ArrayBegin(this.m_SelectedStatistics.Count);
            for (int index = this.m_SelectedStatistics.Count - 1; index >= 0; --index)
            {
                this.BindData(binder, this.m_SelectedStatistics[index]);
            }
            binder.ArrayEnd();
        }

        private void BindData(IJsonWriter binder, Time2WorkStatisticsUISystem.StatItem stat)
        {
            binder.TypeBegin("statistics.ChartDataSets");
            binder.PropertyName("label");
            binder.Write(stat.key);
            binder.PropertyName("data");
            NativeList<Time2WorkStatisticsUISystem.DataPoint> statisticData = this.GetStatisticData(stat);
            binder.ArrayBegin(statisticData.Length);
            for (int index = 0; index < statisticData.Length; ++index)
                binder.Write<Time2WorkStatisticsUISystem.DataPoint>(statisticData[index]);
            binder.ArrayEnd();
            binder.PropertyName("borderColor");
            binder.Write(stat.color.ToHexCode());
            binder.PropertyName("backgroundColor");
            binder.Write(string.Format("rgba({0}, {1}, {2}, 0.5)", (object)Mathf.RoundToInt(stat.color.r * (float)byte.MaxValue), (object)Mathf.RoundToInt(stat.color.g * (float)byte.MaxValue), (object)Mathf.RoundToInt(stat.color.b * (float)byte.MaxValue)));
            binder.PropertyName("fill");
            if (this.m_Stacked)
                binder.Write("origin");
            else
                binder.Write("false");
            binder.TypeEnd();
        }

        private NativeList<Time2WorkStatisticsUISystem.DataPoint> GetStatisticData(
          Time2WorkStatisticsUISystem.StatItem stat)
        {
            this.m_CityStatisticsSystem.CompleteWriters();
            StatisticsPrefab prefab = this.m_PrefabSystem.GetPrefab<StatisticsPrefab>(stat.entity);
            ResourcePrefabs prefabs = this.m_ResourceSystem.GetPrefabs();
            TimeData singleton = TimeData.GetSingleton(this.m_TimeDataQuery);
            int sampleCount = this.m_CityStatisticsSystem.sampleCount;
            //sampleCount = (int)(sampleCount * Mod.m_Setting.slow_time_factor);
            int num1 = (int)(math.min(this.m_SampleRange + 1, sampleCount) * Mod.m_Setting.slow_time_factor);
            if (sampleCount <= 1)
            {
                NativeList<Time2WorkStatisticsUISystem.DataPoint> tempDataPoints = new NativeList<Time2WorkStatisticsUISystem.DataPoint>(1, (AllocatorManager.AllocatorHandle)Allocator.Temp);
                tempDataPoints.Add(new Time2WorkStatisticsUISystem.DataPoint()
                {
                    x = (long)singleton.m_FirstFrame,
                    y = 0L
                });
                return tempDataPoints;
            }
            NativeArray<long> nativeArray1 = CollectionHelper.CreateNativeArray<long>(num1, (AllocatorManager.AllocatorHandle)Allocator.Temp);
            StatisticParameterData[] statisticParameterDataArray1;
            if (!(prefab is ParametricStatistic parametricStatistic))
                statisticParameterDataArray1 = new StatisticParameterData[1]
                {
          new StatisticParameterData() { m_Value = 0 }
                };
            else
                statisticParameterDataArray1 = parametricStatistic.GetParameters().ToArray<StatisticParameterData>();
            StatisticParameterData[] statisticParameterDataArray2 = statisticParameterDataArray1;

            if (stat.isSubgroup && this.m_SelectedStatistics.Count<Time2WorkStatisticsUISystem.StatItem>((Func<Time2WorkStatisticsUISystem.StatItem, bool>)(s => s.isSubgroup)) > 1)
            {
                for (int index1 = 0; index1 < statisticParameterDataArray2.Length; ++index1)
                {
                    int parameter = statisticParameterDataArray2[index1].m_Value;

                    NativeArray<long> nativeArray2 = this.EnsureDataSize(this.m_CityStatisticsSystem.GetStatisticDataArrayLong((StatisticType)stat.statisticType, parameter));
                    for (int index2 = 0; index2 < num1; ++index2)
                    {
                        long num2 = nativeArray2[nativeArray2.Length - num1 + index2];
                        if (stat.statisticType == 4 && prefab is ResourceStatistic resourceStatistic)
                        {
                            Resource resource = EconomyUtils.GetResource(resourceStatistic.m_Resources[index1].m_Resource);
                            ResourceData componentData = this.EntityManager.GetComponentData<ResourceData>(prefabs[resource]);
                            num2 *= (long)(int)EconomyUtils.GetMarketPrice(componentData);
                        }
                        nativeArray1[index2] += num2;
                    }
                }
            }
            else
            {
                int parameter = statisticParameterDataArray2[stat.parameterIndex].m_Value;
                NativeArray<long> statisticDataArrayLong = this.m_CityStatisticsSystem.GetStatisticDataArrayLong((StatisticType)stat.statisticType, parameter);
                NativeArray<long> nativeArray3 = CollectionHelper.CreateNativeArray<long>(0, (AllocatorManager.AllocatorHandle)Allocator.Temp);
                if (stat.statisticType == 16 || stat.statisticType == 15)
                {
                    nativeArray3 = this.EnsureDataSize(this.m_CityStatisticsSystem.GetStatisticDataArrayLong(StatisticType.Population));
                }
                NativeArray<long> nativeArray4 = this.EnsureDataSize(statisticDataArrayLong);
                for (int index = 0; index < num1; ++index)
                {
                    long num3 = nativeArray4[nativeArray4.Length - num1 + index];
                    if (stat.statisticType == 4 && prefab is ResourceStatistic resourceStatistic)
                    {
                        Resource resource = EconomyUtils.GetResource(resourceStatistic.m_Resources[stat.parameterIndex].m_Resource);
                        ResourceData componentData = this.EntityManager.GetComponentData<ResourceData>(prefabs[resource]);
                        num3 *= (long)(int)EconomyUtils.GetMarketPrice(componentData);
                    }
                    if (nativeArray3.Length > 0 && (stat.statisticType == 16 || stat.statisticType == 15))
                    {
                        long num4 = nativeArray3[nativeArray3.Length - num1 + index];
                        if (num4 > 0L)
                            num3 /= num4;
                    }
                    nativeArray1[index] += num3;
                }
            }
            //Mod.log.Info($"Original Samples:{sampleCount}, range:{num1}");
            return this.GetDataPoints(num1, sampleCount, nativeArray1, singleton);
        }

        private NativeArray<long> EnsureDataSize(NativeArray<long> data)
        {
            if (data.Length >= this.m_CityStatisticsSystem.sampleCount)
                return data;
            NativeArray<long> nativeArray = CollectionHelper.CreateNativeArray<long>(this.m_CityStatisticsSystem.sampleCount, (AllocatorManager.AllocatorHandle)Allocator.Temp);
            int num = 0;
            for (int index = 0; index < nativeArray.Length; ++index)
                nativeArray[index] = index >= nativeArray.Length - data.Length ? data[num++] : 0L;
            return nativeArray;
        }

        private NativeList<Time2WorkStatisticsUISystem.DataPoint> GetDataPoints(
          int range,
          int samples,
          NativeArray<long> data,
          TimeData timeData)
        {
            int sampleInterval = this.GetSampleInterval(range);
            
            NativeList<Time2WorkStatisticsUISystem.DataPoint> dataPoints = new NativeList<Time2WorkStatisticsUISystem.DataPoint>(data.Length / sampleInterval, (AllocatorManager.AllocatorHandle)Allocator.Temp);
            int num1 = 0;
            uint x = (uint)math.max((int)this.m_CityStatisticsSystem.GetSampleFrameIndex(samples - range) - (int)timeData.m_FirstFrame, 0);
            x += (uint)(8192f * (Mod.m_Setting.slow_time_factor - 1f));
            //Mod.log.Info($"range:{range}, samples:{samples}, sampleInterval:{sampleInterval}, x:{x}");
            dataPoints.Add(new Time2WorkStatisticsUISystem.DataPoint()
            {
                x = (long)(uint)math.max((long)x, (long)(this.m_TimeUISystem.GetTicks() - (int)(8192f * this.m_SampleRange * Mod.m_Setting.slow_time_factor))),
                y = data[0]
            });
            //Mod.log.Info($"1-x:{(long)(uint)math.max((long)x, (long)(this.m_TimeUISystem.GetTicks() - (int)(8192f * this.m_SampleRange * Mod.m_Setting.slow_time_factor)))},y:{data[0]}");
            if (data.Length > 2)
            {
                
                for (int index = 1; index < data.Length - 1; ++index)
                {
                    if (num1 % sampleInterval == 0)
                    {
                        uint sampleFrameIndex = this.m_CityStatisticsSystem.GetSampleFrameIndex(samples - range + index);
                        sampleFrameIndex += (uint)(8192f * (Mod.m_Setting.slow_time_factor - 1f));
                        //Mod.log.Info($"2-sampleFrameIndex:{sampleFrameIndex},firstFrame:{timeData.m_FirstFrame}");
                        if(sampleFrameIndex < timeData.m_FirstFrame || sampleFrameIndex/ timeData.m_FirstFrame > 500)
                        {
                            sampleFrameIndex = timeData.m_FirstFrame;
                        }
                        dataPoints.Add(new Time2WorkStatisticsUISystem.DataPoint()
                        {
                            x = (long)(sampleFrameIndex - timeData.m_FirstFrame),
                            y = data[index]
                        });
                        //Mod.log.Info($"2-x:{(long)(sampleFrameIndex - timeData.m_FirstFrame)},y:{data[index]}");
                    }
                    ++num1;
                }
            }
            int sampleFrameIndex1 = (int)this.m_CityStatisticsSystem.GetSampleFrameIndex(samples);
            ref NativeList<Time2WorkStatisticsUISystem.DataPoint> local1 = ref dataPoints;
            Time2WorkStatisticsUISystem.DataPoint dataPoint = new Time2WorkStatisticsUISystem.DataPoint();
            dataPoint.x = (long)(uint)(this.m_TimeUISystem.GetTicks() + (int)(183f * Mod.m_Setting.slow_time_factor));
            ref Time2WorkStatisticsUISystem.DataPoint local2 = ref dataPoint;
            ref NativeArray<long> local3 = ref data;
            long num2 = local3[local3.Length - 1];
            local2.y = num2;
            ref Time2WorkStatisticsUISystem.DataPoint local4 = ref dataPoint;
            local1.Add(in local4);
            return dataPoints;
        }

        private int GetSampleInterval(int range)
        {
            int num1 = 32;
            int num2 = range;
            if (num2 <= num1)
                return 1;
            int num3 = num1 - 2;
            return Math.Max(1, (num2 - 2) / num3);
        }

        private void ProcessAddStat(Time2WorkStatisticsUISystem.StatItem stat)
        {
            if (stat.locked)
                return;
            if (stat.category != this.m_ActiveCategory)
            {
                this.m_SelectedStatistics.Clear();
                this.m_SelectedStatisticsTracker.Clear();
                this.m_ActiveCategory = stat.category;
                this.m_ActiveCategoryBinding.Update();
            }

            if (this.m_ActiveGroup == Entity.Null || stat.isGroup || stat.group != this.m_ActiveGroup)
            {
                this.m_SelectedStatistics.Clear();
                this.m_SelectedStatisticsTracker.Clear();
                this.m_ActiveGroup = stat.isGroup ? stat.entity : stat.group;
                this.m_ActiveGroupBinding.Update();
            }
            if (stat.isSubgroup)
            {
                int num = this.m_SelectedStatisticsTracker.Count<Time2WorkStatisticsUISystem.StatItem>((Func<Time2WorkStatisticsUISystem.StatItem, bool>)(s => s.isSubgroup));
                if (num == 1)
                {
                    Time2WorkStatisticsUISystem.StatItem stat1 = this.m_SelectedStatisticsTracker.First<Time2WorkStatisticsUISystem.StatItem>((Func<Time2WorkStatisticsUISystem.StatItem, bool>)(s => s.isSubgroup));
                    this.m_ClearActive = false;
                    this.DeepRemoveStat(stat1);
                    this.AddStat(stat1);
                }
                if (num == 0)
                {
                    this.m_SelectedStatisticsTracker.Add(stat);
                    this.TryAddChildren(stat, this.m_SubGroupCache);
                }
                else
                {
                    this.AddStat(stat);
                }
            }
            else
            {
                if (stat.isGroup)
                {
                    this.m_SelectedStatisticsTracker.Add(stat);
                    if (!this.TryAddChildren(stat, this.m_GroupCache))
                    {
                        this.m_SelectedStatistics.Add(stat);
                    }
                }
                else
                {
                    this.AddStat(stat);
                }
            }
            this.UpdateStackedStatus();
            this.UpdateStats();
        }

        private void UpdateStackedStatus()
        {
            UIStatisticsGroupData component;
            if (this.m_SelectedStatisticsTracker.Count<Time2WorkStatisticsUISystem.StatItem>((Func<Time2WorkStatisticsUISystem.StatItem, bool>)(stat => stat.isSubgroup && stat.group == this.m_ActiveGroup)) > 1 && this.EntityManager.TryGetComponent<UIStatisticsGroupData>(this.m_ActiveGroup, out component))
            {
                this.m_Stacked = component.m_Stacked;
            }
            else
            {
                if (this.m_SelectedStatisticsTracker.Count > 0)
                {
                    this.m_Stacked = false;
                    for (int index = 0; index < this.m_SelectedStatisticsTracker.Count; ++index)
                    {
                        if (this.m_SelectedStatisticsTracker[index].stacked)
                        {
                            this.m_Stacked = true;
                            break;
                        }
                    }
                }
                else
                {
                    this.m_Stacked = false;
                }
            }
            this.m_StackedBinding.Update();
        }

        private void AddStat(Time2WorkStatisticsUISystem.StatItem stat)
        {
            if (!this.m_SelectedStatisticsTracker.Contains(stat))
            {
                this.m_SelectedStatisticsTracker.Add(stat);
            }
            if (this.m_SelectedStatistics.Contains(stat))
                return;
            this.m_SelectedStatistics.Add(stat);
        }

        private bool TryAddChildren(
          Time2WorkStatisticsUISystem.StatItem stat,
          List<Time2WorkStatisticsUISystem.StatItem> cache)
        {
            this.CacheChildren(stat.entity, cache);
            for (int index = 0; index < cache.Count; ++index)
            {
                this.ProcessAddStat(cache[index]);
            }
            return cache.Count > 0;
        }

        private void DeepRemoveStat(Time2WorkStatisticsUISystem.StatItem stat)
        {
            if (!this.m_SelectedStatisticsTracker.Contains(stat))
            {
                int index1 = this.m_SelectedStatisticsTracker.FindIndex((Predicate<Time2WorkStatisticsUISystem.StatItem>)(s => s.entity == stat.group));
                int index2 = this.m_SelectedStatisticsTracker.FindIndex((Predicate<Time2WorkStatisticsUISystem.StatItem>)(s => s.entity == stat.entity && s.isSubgroup));
                if (index1 >= 0)
                {
                    Time2WorkStatisticsUISystem.StatItem stat1 = this.m_SelectedStatisticsTracker[index1];
                    if (index2 >= 0)
                    {
                        Time2WorkStatisticsUISystem.StatItem stat2 = this.m_SelectedStatisticsTracker[index2];
                        this.DeepRemoveStat(stat1);
                        this.ProcessAddStat(stat2);
                    }
                    else
                    {
                        this.DeepRemoveStat(stat1);
                    }
                }
            }
            int num1 = this.m_SelectedStatisticsTracker.Count<Time2WorkStatisticsUISystem.StatItem>((Func<Time2WorkStatisticsUISystem.StatItem, bool>)(s => s.isSubgroup));
            this.RemoveStat(stat);
            if (stat.isGroup)
            {
                for (int index = this.m_SelectedStatistics.Count - 1; index >= 0; --index)
                {
                    if (this.m_SelectedStatistics[index].group == stat.entity)
                    {
                        this.m_SelectedStatistics.RemoveAt(index);
                    }
                }
                for (int index = this.m_SelectedStatisticsTracker.Count - 1; index >= 0; --index)
                {
                    if (this.m_SelectedStatisticsTracker[index].group == stat.entity)
                    {
                        this.m_SelectedStatisticsTracker.RemoveAt(index);
                    }
                }
            }
            if (stat.isSubgroup)
            {
                for (int index = this.m_SelectedStatistics.Count - 1; index >= 0; --index)
                {
                    if (this.m_SelectedStatistics[index].entity == stat.entity)
                    {
                        this.m_SelectedStatistics.RemoveAt(index);
                    }
                }
                for (int index = this.m_SelectedStatisticsTracker.Count - 1; index >= 0; --index)
                {
                    if (this.m_SelectedStatisticsTracker[index].entity == stat.entity)
                    {
                        this.m_SelectedStatisticsTracker.RemoveAt(index);
                    }
                }
            }
            int num2 = this.m_SelectedStatisticsTracker.Count<Time2WorkStatisticsUISystem.StatItem>((Func<Time2WorkStatisticsUISystem.StatItem, bool>)(s => s.isSubgroup));
            if (num1 > 1 && num2 == 1)
            {
                Time2WorkStatisticsUISystem.StatItem stat3 = this.m_SelectedStatisticsTracker.First<Time2WorkStatisticsUISystem.StatItem>((Func<Time2WorkStatisticsUISystem.StatItem, bool>)(s => s.isSubgroup));
                this.RemoveStat(stat3);
                this.ProcessAddStat(stat3);
            }
            if (this.m_ClearActive && this.m_SelectedStatistics.Count == 0 && this.m_SelectedStatisticsTracker.Count <= 1)
            {
                this.ClearStats();
            }
            else
            {
                this.UpdateStats();
            }
            this.m_ClearActive = true;
            this.UpdateStackedStatus();
        }

        private void RemoveStat(Time2WorkStatisticsUISystem.StatItem stat)
        {
            this.m_SelectedStatistics.Remove(stat);
            this.m_SelectedStatisticsTracker.Remove(stat);
        }

        private void ClearStats()
        {
            this.m_SelectedStatistics.Clear();
            this.m_SelectedStatisticsTracker.Clear();
            this.UpdateStats();
            this.ClearActive();
        }

        private void UpdateStats()
        {
            this.m_SelectedStatistics.Sort();
            this.m_SelectedStatisticsBinding.Update();
            this.m_DataBinding.Update();
        }

        private void ClearActive()
        {
            this.m_ActiveGroup = Entity.Null;
            this.m_ActiveGroupBinding.Update();
            this.m_ActiveCategory = Entity.Null;
            this.m_ActiveCategoryBinding.Update();
        }

        private void SetSampleRange(int range)
        {
            this.m_SampleRange = range;
            this.m_SampleRangeBinding.Update(this.m_SampleRange);
            this.UpdateStats();
        }

        [Preserve]
        public Time2WorkStatisticsUISystem()
        {
        }

        public struct StatCategory : IComparable<Time2WorkStatisticsUISystem.StatCategory>
        {
            public Entity m_Entity;
            public PrefabData m_PrefabData;
            public UIObjectData m_ObjectData;

            public StatCategory(Entity entity, UIObjectData objectData, PrefabData prefabData)
            {
                this.m_Entity = entity;
                this.m_PrefabData = prefabData;
                this.m_ObjectData = objectData;
            }

            public int CompareTo(Time2WorkStatisticsUISystem.StatCategory other)
            {
                return this.m_ObjectData.m_Priority.CompareTo(other.m_ObjectData.m_Priority);
            }
        }

        public struct DataPoint : IJsonWritable
        {
            public long x;
            public long y;

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin(this.GetType().FullName);
                writer.PropertyName("x");
                writer.Write((float)this.x);
                writer.PropertyName("y");
                writer.Write((float)this.y);
                writer.TypeEnd();
            }
        }

        public struct StatItem : IJsonReadable, IJsonWritable, IComparable<Time2WorkStatisticsUISystem.StatItem>
        {
            public Entity category;
            public Entity group;
            public Entity entity;
            public int statisticType;
            public int unitType;
            public int parameterIndex;
            public string key;
            public Color color;
            public bool locked;
            public bool isGroup;
            public bool isSubgroup;
            public bool stacked;
            public int priority;

            public StatItem(
              int priority,
              Entity category,
              Entity group,
              Entity entity,
              int statisticType,
              StatisticUnitType unitType,
              int parameterIndex,
              string key,
              Color color,
              bool locked,
              bool isGroup = false,
              bool isSubgroup = false,
              bool stacked = true)
            {
                this.category = category;
                this.group = group;
                this.entity = entity;
                this.statisticType = statisticType;
                this.unitType = (int)unitType;
                this.parameterIndex = parameterIndex;
                this.key = key;
                this.color = color;
                this.locked = locked;
                this.isGroup = isGroup;
                this.isSubgroup = isSubgroup;
                this.stacked = stacked;
                this.priority = priority;
            }

            public void Read(IJsonReader reader)
            {
                long num = (long)reader.ReadMapBegin();
                reader.ReadProperty("category");
                reader.Read(out this.category);
                reader.ReadProperty("group");
                reader.Read(out this.group);
                reader.ReadProperty("entity");
                reader.Read(out this.entity);
                reader.ReadProperty("statisticType");
                reader.Read(out this.statisticType);
                reader.ReadProperty("unitType");
                reader.Read(out this.unitType);
                reader.ReadProperty("parameterIndex");
                reader.Read(out this.parameterIndex);
                reader.ReadProperty("key");
                reader.Read(out this.key);
                reader.ReadProperty("color");
                reader.Read(out this.color);
                reader.ReadProperty("locked");
                reader.Read(out this.locked);
                reader.ReadProperty("isGroup");
                reader.Read(out this.isGroup);
                reader.ReadProperty("isSubgroup");
                reader.Read(out this.isSubgroup);
                reader.ReadProperty("stacked");
                reader.Read(out this.stacked);
                reader.ReadProperty("priority");
                reader.Read(out this.priority);
                reader.ReadMapEnd();
            }

            public void Write(IJsonWriter writer)
            {
                writer.TypeBegin("statistics.StatItem");
                writer.PropertyName("category");
                writer.Write(this.category);
                writer.PropertyName("group");
                writer.Write(this.group);
                writer.PropertyName("entity");
                writer.Write(this.entity);
                writer.PropertyName("statisticType");
                writer.Write(this.statisticType);
                writer.PropertyName("unitType");
                writer.Write(this.unitType);
                writer.PropertyName("parameterIndex");
                writer.Write(this.parameterIndex);
                writer.PropertyName("key");
                writer.Write(this.key);
                writer.PropertyName("color");
                writer.Write(this.color);
                writer.PropertyName("locked");
                writer.Write(this.locked);
                writer.PropertyName("isGroup");
                writer.Write(this.isGroup);
                writer.PropertyName("isSubgroup");
                writer.Write(this.isSubgroup);
                writer.PropertyName("stacked");
                writer.Write(this.stacked);
                writer.PropertyName("priority");
                writer.Write(this.priority);
                writer.TypeEnd();
            }

            public int CompareTo(Time2WorkStatisticsUISystem.StatItem other)
            {
                return this.priority.CompareTo(other.priority);
            }
        }
    }
}
