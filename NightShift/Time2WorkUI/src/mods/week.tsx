import { ModuleRegistryExtend } from "cs2/modding";
import { useValue, bindValue, trigger } from "cs2/api";
import { Button, Icon } from "cs2/ui";
import { time } from "cs2/bindings";
import { getModuleComponent, getModuleClasses } from "../utils/modding";
import styles from "./week.module.scss";
import mod from "../../mod.json";

const toolbarFieldPath =
    "game-ui/game/components/toolbar/components/field/field.tsx";
const fieldStyles = getModuleClasses<{ field: any; content: any }>(
    "game-ui/game/components/toolbar/components/field/field.module.scss",
);
const Divider = getModuleComponent(toolbarFieldPath, "Divider");

// When pausing, time.simulationSpeed$ returns `m_SpeedBeforePause`.
// But we want raw selectedSpeed value. So use another binding.
const dayOfWeek$ = bindValue<string>(mod.id, "dayOfWeek");
//const displayOnlyMode$ = bindValue<boolean>(mod.id, "displayOnlyMode");

const requestRefresh = () => trigger(mod.id, "refresh");

export const week: ModuleRegistryExtend =
    (Component) => (props) => {
        const { children, ...otherProps } = props || {};

        const dayOfWeek = useValue(dayOfWeek$);
        //const paused = useValue(time.simulationPaused$);

        dayOfWeek$.subscribe(requestRefresh);

        const center = (
            <div className={styles["center-box"]}>
                <span>{dayOfWeek}</span>
            </div>
        );
        return (
            <>
                <Component {...otherProps}>{children}</Component>
                <div
                    className={`${fieldStyles.field} ${styles["week"]}`}
                >
                    {center}
                </div>
            </>
        );
    };
