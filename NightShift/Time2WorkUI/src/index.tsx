
import { getModule, type ModRegistrar } from 'cs2/modding';
import { Button } from 'cs2/ui';
import { type ReactElement, useEffect, useState } from 'react';
import timeControlsStyles from 'mods/time-controls.module.scss';
import mod from "../mod.json";
import { useValue, bindValue, trigger } from "cs2/api";
import RealisticTripsMenu from "./mods/RealisticTripsContent/RealisticTripsMenu";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import { CitizenScheduleSection } from './mods/CitizenScheduleSection';


const coTimeControlsStyles: Record<string, string> = getModule(
    'game-ui/game/components/toolbar/bottom/time-controls/time-controls.module.scss',
    'classes'
);

export const register: ModRegistrar = moduleRegistry => {
    VanillaComponentResolver.setRegistry(moduleRegistry);
    moduleRegistry.extend(
        'game-ui/game/components/toolbar/bottom/time-controls/time-controls.tsx',
        'TimeControls',
        COTimeControls => props => (
            <TimeControlsPortal>
                <COTimeControls {...props} />
            </TimeControlsPortal>
        )
    );
    moduleRegistry.extend("game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx", 'selectedInfoSectionComponents', CitizenScheduleSection);


    moduleRegistry.extend(
        'game-ui/game/components/toolbar/bottom/time-controls/time-controls.module.scss',
        timeControlsStyles
    );

    moduleRegistry.append('GameTopLeft', RealisticTripsMenu);
};

function TimeControlsPortal(props: { children: ReactElement }): ReactElement {
    // REPLACE THESE STATES WITH BINDINGS
    const dayOfWeek$ = bindValue<string>(mod.id, "dayOfWeek");
    const requestRefresh = () => trigger(mod.id, "refresh");


    const [isDayDisplayed, setIsDayDisplay] = useState(true);
    //const [currentDate, setCurrentDate] = useState(dayOfWeek$);
    const dayOfWeek = useValue(dayOfWeek$);
    dayOfWeek$.subscribe(requestRefresh);

    const [dateLabelEl, setDateLabelEl] = useState<{
        timeControls: HTMLElement;
        vanilla: HTMLElement;
        modded: HTMLElement;
    }>();

    useEffect(() => {
        if (!(coTimeControlsStyles.timeControls && coTimeControlsStyles.date)) {
            return console.error(
                'Cannot resolve vanilla classes .time-controls and .date.'
            );
        }

        const [timeControls] = document.getElementsByClassName(
            // biome-ignore lint/style/noNonNullAssertion: <explanation>
            coTimeControlsStyles.timeControls.split(' ')[0]!
        );

        if (!(timeControls instanceof HTMLElement)) {
            return console.error(`Cannot find time controls element.`);
        }

        const [dateEl] = timeControls.getElementsByClassName(
            // biome-ignore lint/style/noNonNullAssertion: <explanation>
            coTimeControlsStyles.date.split(' ')[0]!
        );

        if (!(dateEl instanceof HTMLElement)) {
            return console.error(`Cannot find time controls date element.`);
        }

        const modDateEl = document.createElement('div');
        modDateEl.className = coTimeControlsStyles.date ?? '';

        dateEl.insertAdjacentElement('afterend', modDateEl);

        setDateLabelEl({
            timeControls,
            vanilla: dateEl,
            modded: modDateEl
        });
    }, []);

    useEffect(() => {
        if (!dateLabelEl) {
            return;
        }

        dateLabelEl.vanilla.style.display = isDayDisplayed ? 'none' : 'block';
        dateLabelEl.modded.style.display = isDayDisplayed ? 'block' : 'none';

        if (dateLabelEl.timeControls.style.width == 'undefined') {
            dateLabelEl.timeControls.style.width = `calc(3.5em + 300px)`;
        } else {
            dateLabelEl.timeControls.style.width = '';
        }
        //dateLabelEl.timeControls.style.width = `calc(3.5em + 300px)`;
        //if (isDayDisplayed && !dateLabelEl.timeControls.style.width) {
        //    dateLabelEl.timeControls.style.width = `calc(3.5em + ${dateLabelEl.timeControls.offsetWidth}px)`;
        //} else {
        //    dateLabelEl.timeControls.style.width = '';
        //}
    }, [isDayDisplayed, dateLabelEl]);

    useEffect(() => {
        if (!dateLabelEl) {
            return;
        }

        // Note: innerText doesn't work on cohtml
        dateLabelEl.modded.innerHTML = dayOfWeek;
    }, [dayOfWeek, dateLabelEl]);

    return (
        <>
            {props.children}
        </>
    );
}

export default register;

