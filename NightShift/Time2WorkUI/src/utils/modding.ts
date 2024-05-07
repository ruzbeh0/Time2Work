import { getModule } from "cs2/modding";
export { getModule };

export type Component<Props = any> = (props: Props) => JSX.Element;
export const getModuleComponent = <Props = any>(
    modulePath: string,
    exportName: string,
) => getModule(modulePath, exportName) as Component<Props>;

export type Classes<T extends object = object> = T;
export const getModuleClasses = <T extends object = object>(
    modulePath: string,
) => getModule(modulePath, "classes") as Classes<T>;
