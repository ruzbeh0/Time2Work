import engine from "cohtml/cohtml"

export enum NameType {
    Custom = "names.CustomName",
    Localized = "names.LocalizedName",
    Formatted = "names.FormattedName"
}

export type NameCustom = {
    __Type: NameType.Custom,
    name: string
}
export type NameLocalized = {
    __Type: NameType.Localized,
    nameId: string
}
export type NameFormatted = {
    __Type: NameType.Formatted,
    nameId: string,
    nameArgs?: string[]
}

export type ValuableName = NameCustom | NameLocalized | NameFormatted

export function nameToString(nameObj: ValuableName) {
    if (!nameObj) return;
    if (nameObj.__Type == NameType.Custom) {
        return nameObj.name;
    }
    var n = engine.translate(nameObj.nameId);
    if (n != null) {
        if (nameObj.__Type == NameType.Formatted) {
            var r = null != nameObj.nameArgs ? translateArgs(nameObj.nameArgs) : null;
            if (null != r) return replaceArgs(n, r)
        } return n
    } return nameObj.nameId
}
function translateArgs(nameArgs: string[]): Record<string, string> {
    const kv: Record<string, string> = {}
    for (let i = 0; i + 1 < nameArgs.length; i += 2) {
        const key = nameArgs[i];
        const value = nameArgs[i + 1];
        kv[key] = engine.translate(value) ?? value
    }
    return kv;
}
export function replaceArgs(template: string, args: Record<string, any>) {
    return template.replace(/{([\w$]+)}/g, (function (original, n) {
        var replacement = args[n];
        return "string" == typeof replacement ? replacement : original
    }))
}
