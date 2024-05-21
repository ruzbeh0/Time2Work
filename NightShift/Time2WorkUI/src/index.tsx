import { ModRegistrar } from "cs2/modding";
import { week } from "mods/week";

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.extend(
        "game-ui/game/components/toolbar/bottom/time-controls/time-controls.tsx",
        "TimeControls",
        week,
    );
}

export default register;