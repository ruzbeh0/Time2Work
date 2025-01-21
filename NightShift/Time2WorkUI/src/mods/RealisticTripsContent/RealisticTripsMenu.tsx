import React, { useCallback, useState, FC } from 'react';
import { Button, FloatingButton, Tooltip } from "cs2/ui";
import icon from "images/realistictrips.svg";
import styles from "./RealisticTripsMenu.module.scss";
import SpecialEvents from "mods/RealisticTripsSections/SpecialEventSection/SpecialEvents";


// Define the Section type
type Section = 'Special Events';

// Define a new type for components that accept an onClose prop
type SectionComponentProps = {
  onClose: () => void;
};

// Update the sections array type
const sections: { name: Section; displayName: string; component: FC<SectionComponentProps> }[] = [
  { name: 'Special Events', displayName: 'SpecialEvents', component: SpecialEvents },
];

const RealisticTripsButton: FC = () => {
  const [mainMenuOpen, setMainMenuOpen] = useState<boolean>(false);
  const [openSections, setOpenSections] = useState<Record<Section, boolean>>({
    'Special Events': false,
});

  const toggleMainMenu = useCallback(() => {
    setMainMenuOpen(prev => !prev);
  }, []);

  const toggleSection = useCallback((section: Section, isOpen?: boolean) => {
    setOpenSections(prev => ({
      ...prev,
      [section]: isOpen !== undefined ? isOpen : !prev[section],
    }));
  }, []);

  return (
    <div>
      <Tooltip tooltip="Realistic Trips">
        <FloatingButton onClick={toggleMainMenu} src={icon} aria-label="Toggle Realistic Trips Menu" />
      </Tooltip>

      {mainMenuOpen && (
        <div
          draggable={true}
          className={styles.panel}
        >
          <header className={styles.header}>
            <h2>Realistic Trips</h2>
          </header>
          <div className={styles.buttonRow}>
            {sections.map(({ name }) => (
              <Button
                key={name}
                variant='flat'
                aria-label={name}
                aria-expanded={openSections[name]}
                className={
                  openSections[name] ? styles.buttonSelected : styles.Time2WorkButton
                }
                onClick={() => toggleSection(name)}
                onMouseDown={(e) => e.preventDefault()}
              >
                {name}
              </Button>
            ))}
          </div>
        </div>
      )}

      {sections.map(({ name, component: Component }) => (
        openSections[name] && (
          <Component key={name} onClose={() => toggleSection(name, false)} />
        )
      ))}
    </div>
  );
};

export default RealisticTripsButton;
