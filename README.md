# Realistic Trips (Time2Work)

Realistic Trips is a Cities: Skylines II mod that reshapes how citizens and visitors in your city decide *when* and *why* to travel.  
Instead of a static vanilla profile, it models trips with realistic timing patterns for work, school, shopping, leisure, commuters, and tourists.

## What this mod changes

- Reworks daily trip timing and distribution across the day/week.
- Replaces vanilla-like average behavior with more realistic patterns (rush hour peaks, quieter nights, lunch breaks, weekly rhythms, and weekend changes).
- Adds controls for performance vs realism so you can tune simulation load.

## Key systems

### Work

- **Work arrival/departure**: Uses variation based on normal distributions to spread commutes.
- **Peak spreading**: Long-commute workers can leave earlier to avoid peak congestion.
- **Three-shift model**: Day, Evening, and Night shift percentages are configurable.
- **Lunch breaks**: Some workers can leave during the day for shopping or leisure and return to work.
- **Work times + part-time/remote settings**: Controls start/end windows, part-time split, and remote-work share.

### School

- Vacation and weekend logic for schooling.
- School start/end variability (higher education can mean more variation).
- Per-weekday attendance controls for elementary, high school, and tertiary levels.

### Leisure & shopping

- Optional suppression of very early-hour shopping/leisure trips.
- Vacation/holiday behavior that lowers work/school pressure.
- Configurable average shopping duration by resource type.

### Week & day simulation

- Modes for Average Day, Weekday, Saturday, Sunday, and full 7-day week behavior.
- Optional slower calendar pace (`Days per Month` changes game aging scale).
- Optional longer day simulation to better align trip times with in-game travel times.

### External trips

- Weekend/weekday variation for tourists and commuters.
- Optional commuter arrival-by-plane behavior.

### Traffic and events

- Fine-tune traffic reduction factor (simulation sampling intensity).
- Configurable special events at park facilities:
  - minimum attraction and event count by weekday
  - New Year’s Day scheduling behavior

## Presets and controls

The settings UI includes shared presets:

- **Performance**: lighter simulation profile with lower impact.
- **Balanced**: moderate realism with manageable performance.
- **Country**: data-informed daily patterns from multiple countries (Argentina, Australia, Brazil, Canada, France, Germany, India, Italy, Japan, Mexico, Netherlands, Philippines, Poland, Singapore, South Africa, Spain, UK, USA).

For full control, each section can also be adjusted manually.

## Compatibility and caveats

- Compatible with many mods, but realistic behavior increases simulation work in larger cities.
- The combination with Traffic Simulation Adjuster can produce stronger performance effects.
- Existing cities may take a day or two in-universe to settle into new trip patterns after enabling.

## Mod metadata

- **Mod ID**: 77171  
- **Current version**: 2.9.3  
- **Game version target**: 1.6.*  
- **Forum thread**: [Paradox forum](https://forum.paradoxplaza.com/forum/threads/time2work-mod.1655887/)  
- **GitHub**: [github.com/ruzbeh0/Time2Work](https://github.com/ruzbeh0/Time2Work)  
- **Discord**: [Channel link](https://discord.com/channels/1024242828114673724/1224077120641302558)
