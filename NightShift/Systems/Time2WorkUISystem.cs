using Colossal.UI.Binding;
using Game.UI;
using Game.Simulation;
using Unity.Mathematics;
using System;
using Time2Work.Systems;
using Time2Work.Utils;

namespace Time2Work
{
    public partial class Time2WorkUISystem : UISystemBase
    {
        private Throttle _weekUpdateThrottle;
        private ValueBinding<string> _weekDay;

        public string BindGroupName => Mod.harmonyID;


        private void Refresh()
        {
            
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            AddBinding(_weekDay = new ValueBinding<string>(BindGroupName, "dayOfWeek", WeekSystem.getDayOfWeek()));
            _weekUpdateThrottle = Throttle.BySeconds(1, () => { _weekDay.Update(WeekSystem.getDayOfWeek()); });
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            _weekUpdateThrottle.Update(World.Time.DeltaTime);
        }
    }
}
