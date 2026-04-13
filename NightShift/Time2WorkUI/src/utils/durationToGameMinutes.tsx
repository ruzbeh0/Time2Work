import { time } from "cs2/bindings";

export function durationToGameMinutes(duration: number): number {
    return duration * 60 / time.timeSettings$.value.ticksPerDay * 1440;
}
