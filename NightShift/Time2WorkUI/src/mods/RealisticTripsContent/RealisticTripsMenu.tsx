import React, { useCallback, useState, FC } from 'react';
import { Button, FloatingButton, Tooltip, Portal, PanelProps } from "cs2/ui";
import icon from "images/realistictrips.svg";
import styles from "./RealisticTripsMenu.module.scss";
import SpecialEvents from "mods/RealisticTripsSections/SpecialEventSection/SpecialEvents";
import useDataUpdate from "mods/use-data-update";
import { SpecialEventValues } from "mods/RealisticTripsSections/SpecialEventSection/SpecialEvents";
import { formatWords } from "utils/FormatText";
// Define the Section type
type Section = 'Special Events';

// Define a new type for components that accept an onClose prop
interface SectionComponentProps extends PanelProps{}

const formatTime = (hour: number, minutes: number) => {
  const h = String(hour).padStart(2, '0');
  const m = String(minutes).padStart(2, '0');
  return `${h}:${m}`;
};

const allSections: {
  name: Section;
  displayName: string;
  component: FC<SectionComponentProps>;
}[] = [
  { name: "Special Events", displayName: "Special Events", component: SpecialEvents },
];

const RealisticTripsButton: FC = () => {
  const [mainMenuOpen, setMainMenuOpen] = useState<boolean>(false);
  const [openSections, setOpenSections] = useState<Record<Section, boolean>>({
    "Special Events": false,
  });
  const [specialEvents, setSpecialEvents] = useState<SpecialEventValues[]>([]);
  useDataUpdate('specialEventInfo.specialEventDetails', setSpecialEvents);
  
  // Filter events that have a location
  const filteredEvents = specialEvents.filter(event => event.event_location && event.event_location.trim() !== "");

  const toggleMainMenu = useCallback(() => {
    setMainMenuOpen(prev => !prev);
  }, []);

  const toggleSection = useCallback((section: Section, isOpen?: boolean) => {
    setOpenSections(prev => ({
      ...prev,
      [section]: isOpen !== undefined ? isOpen : !prev[section],
    }));
  }, []);

  const tooltipContent = (
  <div style={{ minWidth: '250px' }}>
    <div style={{ fontWeight: 'bold', marginBottom: '0.5rem' }}>Realistic Trips - Special Events</div>
    {filteredEvents.length === 0 ? (
      <div>No Events</div>
    ) : (
      <ul style={{ paddingLeft: '0', listStyleType: 'none', margin: '0' }}>
        {filteredEvents.map((event, idx) => (
          <li
            key={idx}
            style={{
              marginBottom: '0.5rem',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
            }}
          >
            <strong
              style={{
                marginRight: '10rem', 
                flex: '1',
              }}
            >
              {formatWords(event.event_location)}
            </strong>
            <span style={{ whiteSpace: 'nowrap' }}>
              {formatTime(event.start_hour, event.start_minutes)} -{' '}
              {formatTime(event.end_hour, event.end_minutes)}
            </span>
          </li>
        ))}
      </ul>
    )}
  </div>
);

  return (
    <div>
      {/* 5. Use the "tooltip" prop with our dynamic list of events */}
      <Tooltip tooltip={tooltipContent}>
        <FloatingButton
          onClick={toggleMainMenu}
          src={icon}
          aria-label="Toggle Realistic Trips Menu"
        />
      </Tooltip>

      {mainMenuOpen && (
        <div
          draggable={true}
          className={styles.panel}
        >
          <header className={styles.header}>
            <div>Realistic Trips</div>
          </header>
          <div className={styles.buttonRow}>
            {allSections.map(({name, displayName}) => (
              <Button
                key={name}
                variant="flat"
                aria-label={displayName}
                aria-expanded={openSections[name]}
                className={`${styles.Time2WorkButton} ${openSections[name] ? styles.buttonSelected : ''}`}
                onClick={() => toggleSection(name)}
                onMouseDown={e => e.preventDefault()}
              >
                {displayName}
              </Button>
            ))}
          </div>
        </div>
      )}

      {allSections.map(({name, component: Component}) => (
        openSections[name] && (
          <Portal key={name}>
            <Component onClose={() => toggleSection(name, false)}/>
          </Portal>
        )
      ))}
    </div>
  );
};

export default RealisticTripsButton;
