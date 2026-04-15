import { getModule, type ModRegistrar } from "cs2/modding";
import { bindValue, trigger, useValue } from "cs2/api";
import { type ReactElement, useEffect, useState } from "react";

import timeControlsStyles from "mods/time-controls.module.scss";
import mod from "../mod.json";

import RealisticTripsMenu from "./mods/RealisticTripsContent/RealisticTripsMenu";
import { VanillaComponentResolver } from "VanillaComponentResolver";
import { CitizenScheduleSection } from "./mods/CitizenScheduleSection";

const coTimeControlsStyles: Record<string, string> = getModule(
    "game-ui/game/components/toolbar/bottom/time-controls/time-controls.module.scss",
    "classes"
);

// Stable binding instance shared by both portals
const dayOfWeek$ = bindValue<string>(mod.id, "dayOfWeek");

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

function TimeControlsPortal(props: { children: ReactElement }): ReactElement {
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

        const modDateEl = document.createElement("div");
        modDateEl.className = coTimeControlsStyles.date ?? "";
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
        dateLabelEl.timeControls.style.width = "calc(3.5em + 300px)";
        dateLabelEl.timeControls.style.overflow = "visible";
    }, [dateLabelEl]);

    useEffect(() => {
        if (!dateLabelEl) return;
        dateLabelEl.modded.innerHTML = dayOfWeek ?? "";
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

function TimeControlsNewPortal(props: { children: ReactElement }): ReactElement {
    const dayOfWeek = useValue(dayOfWeek$);

    const [dateLabelEl, setDateLabelEl] = useState<{
        container: HTMLElement;
        vanilla: HTMLElement;
        modded: HTMLElement;
    }>();

    useEffect(() => {
        let disposed = false;
        let tries = 0;
        let timer: number | undefined;

        const tryAttach = () => {
            if (disposed) return;

            const roots = Array.from(document.querySelectorAll("div")) as HTMLElement[];

            const candidate = roots.find((el) => {
                const txt = (el.textContent ?? "").trim();
                if (!txt) return false;

                const looksLikeYear = /\b20\d{2}\b/.test(txt) || /\b19\d{2}\b/.test(txt);
                const hasTimeLike = /\b\d{1,2}:\d{2}\b/.test(txt);

                const rect = el.getBoundingClientRect();
                const nearBottom = rect.bottom > window.innerHeight - 220;
                const wideEnough = rect.width > 120;

                return looksLikeYear && hasTimeLike && nearBottom && wideEnough;
            });

            if (!(candidate instanceof HTMLElement)) {
                tries++;
                if (tries < 40) {
                    timer = window.setTimeout(tryAttach, 250);
                } else {
                    console.error("Cannot find new UI time/date element.");
                }
                return;
            }

            const existing = candidate.parentElement?.querySelector(
                "[data-time2work-new-date='true']"
            );

            if (existing instanceof HTMLElement) {
                setDateLabelEl({
                    container: candidate.parentElement as HTMLElement,
                    vanilla: candidate,
                    modded: existing
                });
                return;
            }

            const modDateEl = document.createElement("div");
            modDateEl.setAttribute("data-time2work-new-date", "true");
            modDateEl.style.display = "flex";
            modDateEl.style.alignItems = "center";
            modDateEl.style.justifyContent = "center";
            modDateEl.style.textAlign = "center";
            modDateEl.style.minWidth = "160rem";
            modDateEl.style.width = "max-content";
            modDateEl.style.maxWidth = "none";
            modDateEl.style.whiteSpace = "nowrap";
            modDateEl.style.overflow = "visible";
            modDateEl.style.textOverflow = "clip";
            modDateEl.style.flex = "0 0 auto";


            candidate.insertAdjacentElement("afterend", modDateEl);

            setDateLabelEl({
                container: candidate.parentElement as HTMLElement,
                vanilla: candidate,
                modded: modDateEl
            });
        };

        tryAttach();

        return () => {
            disposed = true;
            if (timer !== undefined) {
                window.clearTimeout(timer);
            }
        };
    }, []);

    useEffect(() => {
        if (!dateLabelEl) return;

        dateLabelEl.vanilla.style.display = "none";
        dateLabelEl.modded.style.display = "flex";

        if (dateLabelEl.container) {
            dateLabelEl.container.style.minWidth = "160rem";
            dateLabelEl.container.style.width = "auto";
            dateLabelEl.container.style.overflow = "visible";
            dateLabelEl.container.style.display = "flex";
            dateLabelEl.container.style.alignItems = "center";
            dateLabelEl.container.style.justifyContent = "center";
            dateLabelEl.container.style.gap = "8px";
        }
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