
import { LocalizedNumber, LocElement, Unit } from "cs2/bindings";
import { Localization, LocComponent, useCachedLocalization } from "cs2/l10n";
import { getModule } from "cs2/modding";
import { Color01, ColorHSVA } from "./ColorUtils";

const registryIndex = {
    color: {
        tsx: "game-ui/common/color.ts",
        fn: {
            "rgb": null as unknown as (r255: number, g255: number, b255: number) => Color01,
            "buildCssLinearGradient": null as unknown as (e: { stops: { color: (string | Color01), offset: number }[] }, mode: string) => string,
            "formatColor": null as unknown as (rgba: Color01) => string,
            "formatHexColor": null as unknown as (rgba: Color01) => string,
            "hsvaToRgba": null as unknown as (hsva: ColorHSVA) => Color01,
            "rgbaToHsva": null as unknown as (hsva: Color01, prevHue?: number) => ColorHSVA,
            "isAchromatic": null as unknown as (rgba: Color01) => boolean,
            "isColorEqual": null as unknown as (rgba: Color01, rgba2: Color01) => boolean,
            "lerpColor": null as unknown as (src: Color01, dst: Color01, factor: number) => Color01,
            "lerpHsva": null as unknown as (src: ColorHSVA, dst: ColorHSVA, factor: number) => ColorHSVA,
            "parseHexColor": null as unknown as (strColor: string) => Color01,
            "parseRgba": null as unknown as (strColor: string) => Color01,

        }
    },
    locComponent: {
        tsx: "game-ui/common/localization/loc-component.tsx",
        fn: {
            "renderLocElement": null as unknown as (e: LocComponent<any>, t: LocElement, n?: string) => any,
            "areLocElementsEqual": null as unknown as (a: LocComponent<any>, b: LocComponent<any>) => boolean,
            "createLocComponent": null as unknown as (displayName: LocComponent<any>['displayName'], renderString: LocComponent<any>['renderString'], propsAreEqual: LocComponent<any>['propsAreEqual']) => LocComponent<any>
        }
    },
    measureUnits: {
        tsx: "game-ui/common/localization/units-us-customary.ts", fn: {
            "fahrenheit": null as unknown as (x: number) => number,
            "kelvin": null as unknown as (x: number) => number,
            "gallons": null as unknown as (x: number) => number,
            "pounds": null as unknown as (x: number) => number,
            "shortTons": null as unknown as (x: number) => number,
            "yards": null as unknown as (x: number) => number,
            "miles": null as unknown as (x: number) => number,
            "squareFeet": null as unknown as (x: number) => number,
            "acres": null as unknown as (x: number) => number,
        }
    },
    unit: {
        tsx: "game-ui/common/localization/unit.ts", fn: {
            "Unit": null as unknown as typeof Unit,
        }
    },
    localizedNumber: {
        tsx: "game-ui/common/localization/localized-number.tsx", fn: {
            "LocalizedNumber": null as unknown as LocalizedNumber,
            "CustomLocalizedNumber": null as unknown as LocalizedNumber,
            "useNumberFormat": null as unknown as (unit: Unit, forceSignal: boolean) => (value: number) => string,
            "formatInteger": null as unknown as (unit: Unit, value: number, forceSign?: boolean, thousandSeparator?: boolean) => string,
            "formatFloat": null as unknown as (locale: Localization, value: number, forceSign?: boolean, significantDitigs?: number, thousandSeparator?: boolean, simplifyZeroes?: boolean, thresoldDecimals?: number) => string,
        }
    },
    localization: {
        tsx: "game-ui/common/localization/localization.tsx",
        fn: {
            useCachedLocalization: null as unknown as typeof useCachedLocalization
        }
    },
    time: {
        tsx: "game-ui/common/charts/simulation-time-scale.ts", fn: {

        }
    }
}

type RegistryType = typeof registryIndex;

export class VanillaFnResolver {
    public static get instance(): VanillaFnResolver { return this._instance ??= new VanillaFnResolver() }
    private static _instance?: VanillaFnResolver



    private cachedData: Partial<Record<string, any>> = {}
    private getOrUpdateCache<T extends keyof RegistryType>(entry: T, fn: string) {
        return this.cachedData[entry + "|" + fn] ?? this.updateCache(entry, fn)
    }
    private updateCache<T extends keyof typeof registryIndex>(entry: T, fn: string) {
        const entryData = registryIndex[entry];
        return this.cachedData[entry + "|" + fn] = getModule(entryData.tsx, fn)
    }

    public get localeFn() {
        return Object.fromEntries(Object.keys(registryIndex.locComponent.fn).map(x => [x, this.getOrUpdateCache("locComponent", x)])) as RegistryType['locComponent']['fn']
    }
    public get measureUnits() {
        return Object.fromEntries(
            Object.keys(registryIndex.measureUnits.fn).map(x => [x, this.getOrUpdateCache("measureUnits", x)])
                .concat(Object.keys(registryIndex.unit.fn).map(x => [x, this.getOrUpdateCache("measureUnits", x)]))
        ) as RegistryType['measureUnits']['fn'] & RegistryType['unit']['fn']
    }
    public get unit() {
        return Object.fromEntries(Object.keys(registryIndex.unit.fn).map(x => [x, this.getOrUpdateCache("unit", x)])) as RegistryType['unit']['fn'];
    }
    public get localizedNumber() {
        return Object.fromEntries(Object.keys(registryIndex.localizedNumber.fn).map(x => [x, this.getOrUpdateCache("localizedNumber", x)])) as RegistryType['localizedNumber']['fn']
    }
    public get localization() {
        return Object.fromEntries(Object.keys(registryIndex.localization.fn).map(x => [x, this.getOrUpdateCache("localization", x)])) as RegistryType['localization']['fn']
    }
    public get color() {
        return Object.fromEntries(Object.keys(registryIndex.color.fn).map(x => [x, this.getOrUpdateCache("color", x)])) as RegistryType['color']['fn']
    }
}




