// Workplaces.tsx
import React, { FC, useCallback, useEffect, useState } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';


// Define interfaces for component props
interface SpecialEventValues {
    start_hour: number;
    start_minutes: number;
    end_hour: number;
    end_minutes: number;
    event_location: string;
    [key: string]: any;
}
interface SpecialEventProps {
    levelColor?: string;
    levelValues: SpecialEventValues;
    showAll?: boolean;
}

// SpecialEvent Component
const SpecialEventLevel: React.FC<SpecialEventProps> = ({
    levelColor,
    levelValues,
    showAll = true,
}) => {
    if (showAll) {
        return (
            <div
                className="labels_L7Q row_S2v"
                style={{ width: '99%', paddingTop: '1rem', paddingBottom: '1rem' }}
            >
                <div style={{ width: '1%' }}></div>
                <div style={{ alignItems: 'left', width: '44%' }}>
                    <div>{levelValues['event_location']}</div>
                </div>
                <div style={{ width: '28%', justifyContent: 'left' }}>
                    {`${levelValues['start_hour']}:${levelValues['start_minutes']}0`}
                </div>
                <div style={{ width: '28%', justifyContent: 'left' }}>
                    {`${levelValues['end_hour']}:${levelValues['end_minutes']}0`}
                </div>
            </div>
        );
    } else {
        return (<div></div>);
    }
};


// Main SpecialEvent Component
interface SpecialEventLevelProps {
    onClose: () => void;
}

// Simple horizontal line
const DataDivider: React.FC = () => {
    return (
        <div style={{ display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center' }}>
            <div style={{ borderBottom: '1px solid gray' }}></div>
        </div>
    );
};

const SpecialEvent: FC<SpecialEventLevelProps> = ({ onClose }) => {
    // State for controlling the visibility of the panel
    const [isPanelVisible, setIsPanelVisible] = useState(true);

    // Data fetching and other logic
    const [SpecialEvent, setSpecialEvent] = useState<SpecialEventValues[]>([]);
    useDataUpdate('specialEventInfo.specialEventDetails', setSpecialEvent);

    const defaultPosition = { top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 } ;
    const [panelPosition, setPanelPosition] = useState(defaultPosition);
    const handleSavePosition = useCallback((position: { top: number; left: number }) => {
        setPanelPosition(position);
    }, []);
    const [lastClosedPosition, setLastClosedPosition] = useState(defaultPosition);

    // Handler for closing the panel
    const handleClose = useCallback(() => {
        setLastClosedPosition(panelPosition); // Save the current position before closing
        setIsPanelVisible(false);
        onClose();
    }, [onClose, panelPosition]);

    useEffect(() => {
        if (!isPanelVisible) {
            setPanelPosition(lastClosedPosition);
        }
    }, [isPanelVisible, lastClosedPosition]);

    if (!isPanelVisible) {
        return null;
    }

    return (
        <$Panel
            title="Special Events"
            onClose={handleClose}
            initialSize={{ width: window.innerWidth * 0.25, height: window.innerHeight * 0.15 }}
            initialPosition={panelPosition}
            onSavePosition={handleSavePosition}
        >
            {SpecialEvent.length === 0 ? (
                <p>No Events Today</p>
            ) : (
                <div>
                    {/* Your existing content rendering */}
                    {/* Adjusted heights as needed */}
                    <div style={{ height: '10rem' }}></div>
                    <div
                        className="labels_L7Q row_S2v"
                        style={{ width: '99%', paddingTop: '1rem', paddingBottom: '1rem' }}
                    >
                        <div style={{ width: '1%' }}></div>
                        <div style={{ alignItems: 'left', width: '44%' }}>
                                <div><b>{"Event Location"}</b></div>
                        </div>
                        <div style={{ width: '28%', justifyContent: 'left' }}>
                                <b>{"Start Time"}</b>
                        </div>
                        <div style={{ width: '28%', justifyContent: 'left' }}>
                                <b>{"End Time"}</b>
                        </div>
                    </div>
                    <DataDivider />
                        <div style={{ height: '5rem' }}></div>
                    <SpecialEventLevel
                         levelValues={SpecialEvent[0]}
                    />
                    <SpecialEventLevel
                         levelValues={SpecialEvent[1]}
                    />
                    <SpecialEventLevel
                         levelValues={SpecialEvent[2]}
                    />
                    <DataDivider />
                </div>
            )}
        </$Panel>
    );
};

export default SpecialEvent;

// Registering the panel with HookUI (if needed)
// window._$hookui.registerPanel({
//     id: 'infoloom.workplaces',
//     name: 'InfoLoom: Workplaces',
//     icon: 'Media/Game/Icons/Workers.svg',
//     component: $Workplaces,
// });
