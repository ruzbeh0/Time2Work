import React, {FC, useCallback, useEffect, useMemo, useRef, useState} from 'react';
import useDataUpdate from 'mods/use-data-update';
import { Panel, Scrollable, Portal, DraggablePanelProps, Number2 } from 'cs2/ui'; // Updated import
import styles from './SpecialEvents.module.scss';
import {formatWords} from "utils/FormatText";

// Define interfaces for component props
export interface SpecialEventValues {
    start_hour: number;
    start_minutes: number;
    end_hour: number;
    end_minutes: number;
    event_location: string;
    [key: string]: any;
}

interface SpecialEventProps extends DraggablePanelProps {

    levelValues: SpecialEventValues;
    showAll?: boolean;
}



const SpecialEventLevel: React.FC<SpecialEventProps> = ({levelValues, showAll = true}) => {
    if (!showAll) return null;

    return (
        <div
            className="labels_L7Q row_S2v"
            style={{
                width: '100%',
                padding: '1rem 25rem',
                display: 'flex',
                alignItems: 'center',
                boxSizing: 'border-box',
            }}
        >
            <div style={{
                flex: '0 0 50%',
                paddingRight: '1rem',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap'
            }}>
                {formatWords(levelValues.event_location)}
            </div>
            <div style={{flex: '0 0 25%', textAlign: 'center'}}>
                {`${levelValues.start_hour}:${levelValues.start_minutes.toString().padStart(2, '0')}`}
            </div>
            <div style={{flex: '0 0 25%', textAlign: 'center'}}>
                {`${levelValues.end_hour}:${levelValues.end_minutes.toString().padStart(2, '0')}`}
            </div>
        </div>
    );
};

// In the main SpecialEvent component, update the header:


// Main SpecialEvent Component
interface SpecialEventLevelProps extends DraggablePanelProps {

}

// Simple horizontal line
const DataDivider: React.FC = () => (
    <div style={{display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center'}}>
        <div style={{borderBottom: '1px solid gray', width: '100%'}}></div>
    </div>
);

const SpecialEvent: FC<SpecialEventLevelProps> = ({onClose, initialPosition, ...props}) => {
    // State for controlling the visibility of the panel
    const [isPanelVisible, setIsPanelVisible] = useState(true);
    const initialPos: Number2 = { x: 0.038, y: 0.15 };
    const panelRef = useRef<HTMLDivElement>(null);

    // Data fetching and other logic
    const [specialEvents, setSpecialEvents] = useState<SpecialEventValues[]>([]);
    useDataUpdate('specialEventInfo.specialEventDetails', setSpecialEvents);





    // Filter out events without a valid event_location
    const filteredSpecialEvents = useMemo(() => {
        return specialEvents.filter(event => {
            return event.event_location && event.event_location.trim() !== '';
        });
    }, [specialEvents]);

    return (
        <Panel

            draggable={true}
            initialPosition={initialPos}
            onClose={onClose}
            className={styles.panel}


            header={(
                <div className={styles.header}>
                    <span className={styles.headerText}>Special Events</span>
                </div>
            )}>




            {filteredSpecialEvents.length === 0 ? (
                <p>No Events Today</p>
            ) : (
                <div>
                    <div style={{maxWidth: '1200px', margin: '0 auto', padding: '0 25rem'}}>
                        <div
                            className="labels_L7Q row_S2v"
                            style={{
                                width: '100%',
                                padding: '1rem 0',
                                display: 'flex',
                                alignItems: 'center',
                            }}
                        >
                            <div style={{flex: '0 0 50%'}}>
                                <div><b>Event Location</b></div>
                            </div>
                            <div style={{flex: '0 0 25%', textAlign: 'center'}}>
                                <b>Start Time</b>
                            </div>
                            <div style={{flex: '0 0 25%', textAlign: 'center'}}>
                                <b>End Time</b>
                            </div>
                        </div>
                    </div>
                    <DataDivider/>

                    {/* Event List */}
                    <div style={{padding: '1rem 0'}}>
                        <Scrollable smooth={true} vertical={true} trackVisibility="scrollable">
                            {filteredSpecialEvents.map((event, index) => (
                                <SpecialEventLevel
                                    key={index}
                                    levelValues={event}
                                />
                            ))}
                        </Scrollable>
                    </div>

                    <DataDivider/>
                </div>
            )}
        </Panel>
    );
};

export default SpecialEvent;
