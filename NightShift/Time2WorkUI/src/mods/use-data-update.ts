import engine from "cohtml/cohtml"
import { useEffect, useMemo } from "react"

const useDataUpdate = (
    event: string,
    onUpdate: (data: any) => void,
    deps: any[] = [] 
) => {
    const updateEvent = useMemo(() => event + ".update", [event])
    const subscribeEvent = useMemo(() => event + ".subscribe", [event])
    const unsubscribeEvent = useMemo(() => event + ".unsubscribe", [event])

    useEffect(() => {
        const handleUpdate = (data: any) => {
            onUpdate && onUpdate(data)
        }

        const sub = engine.on(updateEvent, handleUpdate)
        engine.trigger(subscribeEvent)
        return () => {
            engine.trigger(unsubscribeEvent)
            sub.clear()
        };
    }, deps || [updateEvent, subscribeEvent, unsubscribeEvent])
}

export default useDataUpdate