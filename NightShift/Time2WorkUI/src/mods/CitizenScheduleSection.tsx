import { getModule } from "cs2/modding";
import { Theme } from "cs2/bindings";
import { useValue, trigger, bindValue } from "cs2/api";
import { VanillaComponentResolver } from "mods/VanillaComponentResolver/VanillaComponentResolver";
import mod from "mod.json";
import React from "react";
const selectedCitizen$ = bindValue<CitizenSchedule>(mod.id, "schedule");
const formatTimeWithAMPM = (hour: number, minute: number): string => {
    // Ensure we're working with absolute values
    let absHour = Math.abs(hour);
    const ampm = absHour >= 12 ? 'PM' : 'AM';

    // Convert to 12-hour format
    let displayHour = absHour;
    if (displayHour > 12) {
        displayHour -= 12;
    } else if (displayHour === 0) {
        displayHour = 12;
    }

    return `${displayHour}:${Math.abs(minute).toString().padStart(2, '0')} ${ampm}`;
};
interface InfoSectionComponent {
    group: string;
    tooltipKeys: Array<string>;
    tooltipTags: Array<string>;
}

interface CitizenSchedule{
    student : boolean;
    work_start_hour: number;
    work_end_hour: number;
    work_start_minute: number;
    work_end_minute: number;
    lunch_start_hour: number;
    lunch_end_hour: number;
    lunch_start_minute: number;
    lunch_end_minute: number;
    dayOff: boolean;
    work_from_home: boolean;
}
const InfoRowTheme: Theme | any = getModule(
    "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss",
    "classes"
)

const InfoSection: any = getModule(
    "game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx",
    "InfoSection"
)

const InfoRow: any = getModule(
    "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx",
    "InfoRow"
)
export const CitizenScheduleSection = (componentList: any): any => {
    componentList["Time2Work.Systems.CitizenScheduleSection"] = (e: InfoSectionComponent) => {
        const selectedCitizen = useValue(selectedCitizen$);

        return (
            <InfoSection focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED} disableFocus={true}>
                <InfoRow
                    left={"Schedule"}
                    right={(selectedCitizen.work_from_home ? "Working From Home" : "")}
                    uppercase={true}
                    disableFocus={true}
                    subRow={false}
                    className={InfoRowTheme}
                ></InfoRow>
                {!selectedCitizen.dayOff && (
                    <InfoRow
                        left={selectedCitizen.student ? "Student Hours" : "Work Hours"}
                        right={`${formatTimeWithAMPM(selectedCitizen.work_start_hour, selectedCitizen.work_start_minute)} - ${formatTimeWithAMPM(selectedCitizen.work_end_hour, selectedCitizen.work_end_minute)}`}
                        tooltipKeys={e.tooltipKeys}
                        tooltipTags={e.tooltipTags}
                        disableFocus={true}
                        subRow={true}
                        uppercase={false}
                        className={InfoRowTheme}
                    ></InfoRow>
                )}
                {!selectedCitizen.dayOff && !selectedCitizen.student &&
                    selectedCitizen.lunch_start_hour > 0 &&
                    (
                        <InfoRow
                            left={"Lunch Hours"}
                            right={`${formatTimeWithAMPM(selectedCitizen.lunch_start_hour, selectedCitizen.lunch_start_minute)} - ${formatTimeWithAMPM(selectedCitizen.lunch_end_hour, selectedCitizen.lunch_end_minute)}`}
                            tooltipKeys={e.tooltipKeys}
                            tooltipTags={e.tooltipTags}
                            disableFocus={true}
                            subRow={true}
                            uppercase={false}
                            className={InfoRowTheme}
                        ></InfoRow>
                    )
                }
                {selectedCitizen.dayOff == true && (
                    <InfoRow
                        left={"Taking the Day Off"}
                        tooltipKeys={e.tooltipKeys}
                        tooltipTags={e.tooltipTags}
                        disableFocus={true}
                        subRow={true}
                        uppercase={false}
                        className={InfoRowTheme}
                    ></InfoRow>
                )}
            </InfoSection>
        );
    };
    return componentList as any;
};