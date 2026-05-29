import { getModule, type ModRegistrar } from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { type ReactElement, type ReactNode, useEffect, useState } from "react";

import timeControlsStyles from "mods/time-controls.module.scss";
import mod from "../mod.json";

import RealisticTripsMenu from "./mods/RealisticTripsContent/RealisticTripsMenu";
import { CitizenScheduleSection } from "./mods/CitizenScheduleSection";

const coTimeControlsStyles: Record<string, string> = getModule(
    "game-ui/game/components/toolbar/bottom/time-controls/time-controls.module.scss",
    "classes"
);

// Stable binding instance shared by both portals
const dayOfWeek$ = bindValue<string>(mod.id, "dayOfWeek");

type PortalProps = {
    children?: ReactNode;
};

export const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.extend(
        "game-ui/game/components/toolbar/bottom/time-controls/time-controls.tsx",
        "TimeControls",
        (COTimeControls) => (props) => (
            <TimeControlsPortal>
                <COTimeControls {...props} />
            </TimeControlsPortal>
        )
    );

    moduleRegistry.extend(
        "game-ui/game/components/toolbar/bottom/time-controls/time-controls-new.tsx",
        "TimeControlsNew",
        (COTimeControlsNew) => (props) => (
            <TimeControlsNewPortal>
                <COTimeControlsNew {...props} />
            </TimeControlsNewPortal>
        )
    );

    moduleRegistry.extend(
        "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
        "selectedInfoSectionComponents",
        CitizenScheduleSection
    );

    moduleRegistry.extend(
        "game-ui/game/components/toolbar/bottom/time-controls/time-controls.module.scss",
        timeControlsStyles
    );

    moduleRegistry.append("GameTopLeft", RealisticTripsMenu);
};

function TimeControlsPortal(props: PortalProps): ReactElement {
    const dayOfWeek = useValue(dayOfWeek$);

    const [dateLabelEl, setDateLabelEl] = useState<{
        timeControls: HTMLElement;
        vanilla: HTMLElement;
        modded: HTMLElement;
    }>();

    useEffect(() => {
        if (!(coTimeControlsStyles.timeControls && coTimeControlsStyles.date)) {
            console.error("Cannot resolve vanilla classes .time-controls and .date.");
            return;
        }

        const [timeControls] = document.getElementsByClassName(
            coTimeControlsStyles.timeControls.split(" ")[0]!
        );

        if (!(timeControls instanceof HTMLElement)) {
            console.error("Cannot find time controls element.");
            return;
        }

        const [dateEl] = timeControls.getElementsByClassName(
            coTimeControlsStyles.date.split(" ")[0]!
        );

        if (!(dateEl instanceof HTMLElement)) {
            console.error("Cannot find time controls date element.");
            return;
        }

        const existing = timeControls.querySelector(
            "[data-time2work-date='true']"
        );

        if (existing instanceof HTMLElement) {
            setDateLabelEl({
                timeControls,
                vanilla: dateEl,
                modded: existing
            });
            return;
        }

        const modDateEl = document.createElement("div");
        modDateEl.setAttribute("data-time2work-date", "true");
        modDateEl.className = coTimeControlsStyles.date ?? "";
        modDateEl.style.display = "none";
        modDateEl.style.whiteSpace = "nowrap";
        modDateEl.style.overflow = "hidden";
        modDateEl.style.textOverflow = "ellipsis";
        modDateEl.style.pointerEvents = "none";

        dateEl.insertAdjacentElement("afterend", modDateEl);

        setDateLabelEl({
            timeControls,
            vanilla: dateEl,
            modded: modDateEl
        });

        return () => {
            if (modDateEl.parentElement) {
                modDateEl.remove();
            }
        };
    }, []);

    useEffect(() => {
        if (!dateLabelEl) return;

        dateLabelEl.vanilla.style.display = "none";
        dateLabelEl.modded.style.display = "block";

        // Keep conservative to avoid layout blowups
        dateLabelEl.timeControls.style.width = "";
        dateLabelEl.timeControls.style.overflow = "";
    }, [dateLabelEl]);

    useEffect(() => {
        if (!dateLabelEl) return;
        dateLabelEl.modded.textContent = dayOfWeek ?? "";
    }, [dayOfWeek, dateLabelEl]);

    useEffect(() => {
        const subscription = dayOfWeek$.subscribe(() => {
            trigger(mod.id, "refresh");
        });

        return () => {
            subscription.dispose();
        };
    }, []);

    return <>{props.children}</>;
}

function TimeControlsNewPortal(props: PortalProps): ReactElement {
    const dayOfWeek = useValue(dayOfWeek$);

    const [dateLabelEl, setDateLabelEl] = useState<{
        vanilla: HTMLElement;
        modded: HTMLElement;
    }>();

    useEffect(() => {
        let disposed = false;
        let tries = 0;
        let timer: number | undefined;
        let createdEl: HTMLElement | undefined;

        const tryAttach = () => {
            if (disposed) return;

            const dateTimeContainerNewClass =
                coTimeControlsStyles.dateTimeContainerNew ??
                coTimeControlsStyles["date-time-container-new"];

            const dateTimeClass =
                coTimeControlsStyles.dateTime ??
                coTimeControlsStyles["date-time"];

            const dateClass = coTimeControlsStyles.date;

            if (!dateTimeContainerNewClass || !dateTimeClass || !dateClass) {
                tries++;
                if (tries < 30) {
                    timer = window.setTimeout(tryAttach, 300);
                }
                return;
            }

            const containerClass = dateTimeContainerNewClass.split(" ")[0]!;
            const innerClass = dateTimeClass.split(" ")[0]!;
            const dateOnlyClass = dateClass.split(" ")[0]!;

            const containers = Array.from(
                document.getElementsByClassName(containerClass)
            ).filter((x): x is HTMLElement => x instanceof HTMLElement);

            let vanillaDateEl: HTMLElement | null = null;

            for (const container of containers) {
                const inner = container.getElementsByClassName(innerClass)[0];
                if (!(inner instanceof HTMLElement)) continue;

                const dateEl = inner.getElementsByClassName(dateOnlyClass)[0];
                if (dateEl instanceof HTMLElement) {
                    vanillaDateEl = dateEl;
                    break;
                }
            }

            if (!(vanillaDateEl instanceof HTMLElement)) {
                tries++;
                if (tries < 30) {
                    timer = window.setTimeout(tryAttach, 300);
                } else {
                    console.error("Cannot find new UI date element.");
                }
                return;
            }

            const parent = vanillaDateEl.parentElement;
            if (!(parent instanceof HTMLElement)) {
                return;
            }

            const existing = parent.querySelector(
                "[data-time2work-new-date='true']"
            );

            if (existing instanceof HTMLElement) {
                setDateLabelEl({
                    vanilla: vanillaDateEl,
                    modded: existing
                });
                return;
            }

            const modDateEl = document.createElement("div");
            modDateEl.setAttribute("data-time2work-new-date", "true");

            // Reuse the same date styling as vanilla new UI
            modDateEl.className = dateClass;
            modDateEl.style.display = "none";
            modDateEl.style.pointerEvents = "none";
            modDateEl.style.whiteSpace = "nowrap";
            modDateEl.style.textAlign = "center";

            vanillaDateEl.insertAdjacentElement("afterend", modDateEl);
            createdEl = modDateEl;

            setDateLabelEl({
                vanilla: vanillaDateEl,
                modded: modDateEl
            });
        };

        tryAttach();

        return () => {
            disposed = true;
            if (timer !== undefined) {
                window.clearTimeout(timer);
            }
            if (createdEl?.parentElement) {
                createdEl.remove();
            }
        };
    }, []);

    useEffect(() => {
        if (!dateLabelEl) return;

        dateLabelEl.vanilla.style.display = "none";
        dateLabelEl.modded.style.display = "block";
    }, [dateLabelEl]);

    useEffect(() => {
        if (!dateLabelEl) return;
        dateLabelEl.modded.textContent = dayOfWeek ?? "";
    }, [dayOfWeek, dateLabelEl]);

    useEffect(() => {
        const subscription = dayOfWeek$.subscribe(() => {
            trigger(mod.id, "refresh");
        });

        return () => {
            subscription.dispose();
        };
    }, []);

    return <>{props.children}</>;
}

export default register;