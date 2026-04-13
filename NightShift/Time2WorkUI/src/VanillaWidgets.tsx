
import { DropdownItem, LocalizedString, LocElement, Theme, UniqueFocusKey } from "cs2/bindings";
import { getModule } from "cs2/modding";
import { CSSProperties, HTMLAttributes, MutableRefObject, ReactNode, useState } from "react";
import { VanillaComponentResolver } from "./VanillaComponentResolver";
import "./common.scss"
import { FocusDisabled } from "cs2/input";

export type UIColorRGBA = {
    r: number
    g: number
    b: number
    a: number
}
export enum LocElementType {
    Bounds = "Game.UI.Localization.LocalizedBounds",
    Fraction = "Game.UI.Localization.LocalizedFraction",
    Number = "Game.UI.Localization.LocalizedNumber",
    String = "Game.UI.Localization.LocalizedString"
}

type PropsColorPicker = {
    label?: string | JSX.Element | JSX.Element[]
    value: UIColorRGBA
    showAlpha?: boolean
    disabled?: boolean
    onChange: (newVal: UIColorRGBA) => any
} & Omit<HTMLAttributes<any>, "onChange">

type PropsIntSlider = {
    label?: string | JSX.Element | JSX.Element[]
    value: number
    min: number
    max: number
    disabled?: boolean
    onChange?: (newVal: number) => any
    onChangeStart?: (newVal: number) => any
    onChangeEnd?: (newVal: number) => any
} & Omit<HTMLAttributes<any>, "onChange">

type PropsFloatSlider = {
    label?: string | JSX.Element | JSX.Element[]
    value: number
    min: number
    max: number
    fractionDigits?: number
    disabled?: boolean
    onChange?: (newVal: number) => any
    onChangeStart?: (newVal: number) => any
    onChangeEnd?: (newVal: number) => any
} & Omit<HTMLAttributes<any>, "onChange">


export type PropsDropdownField<T> = {
    items: DropdownItem<T>[],
    value?: T,
    onChange?: (newVal: T) => any
    disabled?: boolean
} & Omit<HTMLAttributes<any>, "onChange">

type PropsEditorItemControl = { label?: string, children?: ReactNode, styleContent?: React.CSSProperties, className?: string }
type PropsFocusableEditorItem = { disabled?: boolean, centered?: boolean, className?: string, focusKey?: UniqueFocusKey, onFocusChange?: () => any, children?: JSX.Element | JSX.Element[] | string }
type PropsDirectoryPickerButton = { label: string, value: string, disabled?: boolean, className?: string, theme?: Theme, onOpenDirectoryBrowser: () => any }
type PropsStringInputField = { ref?: MutableRefObject<HTMLInputElement>, value: string, disabled?: boolean, onChange: (s: string) => any, className?: string, maxLength?: number } & ({
    onChangeStart?: HTMLTextAreaElement['onfocus'], onChangeEnd?: HTMLTextAreaElement['onblur'], multiline: true,
} | {
    onChangeStart?: HTMLInputElement['onfocus'], onChangeEnd?: HTMLInputElement['onblur'], multiline?: false | undefined,
})
type PropsToggleField = { label: string, value: boolean, disabled?: boolean, onChange: (x: boolean) => any }
type PropsFloatInputField = { label: string, value: number, min?: number, max?: number, fractionDigits?: number, disabled?: boolean, onChange: (s: number) => any, className?: string, maxLength?: number } & ({
    onChangeStart?: HTMLTextAreaElement['onfocus'], onChangeEnd?: HTMLTextAreaElement['onblur'], multiline: true,
} | {
    onChangeStart?: HTMLInputElement['onfocus'], onChangeEnd?: HTMLInputElement['onblur'], multiline?: false | undefined,
})
type PropsFloat2InputField = { label: string, value: { x: number, y: number }, disabled?: boolean, onChange: (s: { x: number, y: number }) => any } & ({
    onChangeEnd?: HTMLTextAreaElement['onblur'], multiline: true,
} | {
    onChangeEnd?: HTMLInputElement['onblur'], multiline?: false | undefined,
})
type PropsFloat3InputField = { label: string, value: { x: number, y: number, z: number }, disabled?: boolean, onChange: (s: { x: number, y: number, z: number }) => any } & ({
    onChangeEnd?: HTMLTextAreaElement['onblur'], multiline: true,
} | {
    onChangeEnd?: HTMLInputElement['onblur'], multiline?: false | undefined,
})

export type HierarchyViewport = {
    displayName: LocElement,
    icon?: string,
    tooltip?: LocElement,
    level: number,
    selectable?: boolean,
    selected?: boolean,
    expandable?: boolean,
    expanded?: boolean
}

type PropsHierarchyMenu = {
    viewport: HierarchyViewport[],
    flex: { grow: CSSProperties['flexGrow'], shrink: CSSProperties['flexShrink'], basis: CSSProperties['flexBasis'] },
    visibleCount: number,
    onSelect?: (viewportIndex: number, selected: boolean) => any,
    onSetExpanded?: (viewportIndex: number, expanded: boolean) => any,
    singleSelection?: boolean,
    onRenderedRangeChange?: (startIndex: number, endIndex: number) => any
}

type PropsEditorScrollable = {
    className?: string,
    style?: CSSProperties,
    vertical?: boolean,
    horizontal?: boolean,
    children?: ReactNode
}

type ItemPickerObject = {
    displayName: string,
    image?: string,
    tinted?: boolean,
    directory: boolean,
    favorite?: boolean
}

export type ItemPickerProps = {
    ref?: MutableRefObject<any>
    focusKey?: UniqueFocusKey,
    selectedIndex?: number,
    columnCount: number,
    hasImages?: boolean,
    data: {
        get: (index: number) => ItemPickerObject, length: number
    }
    className?: string,
    onRenderedRangeChange?: () => any,
    onSelect: (x: number) => any,
    selectOnFocus?: boolean,
    onToggleFavorite?: (index: number, newValue: boolean) => any,
}

export type CheckboxProps = {
    showHint?: boolean,
    checked: boolean,
    disabled?: boolean,
    onChange: (value: boolean) => any,
    className?: string
}

export type IntInputStandaloneProps = {
    style?: React.CSSProperties
    className?: string,
    min?: number,
    max?: number,
    value: number,
    disabled?: boolean,
    onChange?: (x: number) => any,
    onFocus?: () => any,
    onBlur?: () => any,
}

export type FloatInputStandaloneProps = {
    className?: string,
    min?: number,
    max?: number,
    fractionDigits?: number,
    value: number,
    disabled?: boolean,
    onChange?: (x: number) => any,
    onFocus?: () => any,
    onBlur?: () => any,
    style?: React.CSSProperties
}

const registryIndex = {
    themeDropdown: ["game-ui/menu/widgets/dropdown-field/dropdown-field.module.scss", "classes"],
    inputField: ["game-ui/debug/widgets/fields/input-field/input-field.module.scss", "classes"],
    ColorPicker: ["game-ui/editor/widgets/fields/color-field.tsx", "ColorField"],
    IntSlider: ["game-ui/editor/widgets/fields/number-slider-field.tsx", "IntSliderField"],
    FloatSlider: ["game-ui/editor/widgets/fields/number-slider-field.tsx", "FloatSliderField"],
    DropdownField: ["game-ui/editor/widgets/fields/dropdown-field.tsx", "DropdownField"],
    FocusableEditorItem: ["game-ui/editor/widgets/item/editor-item.tsx", "FocusableEditorItem"],
    editorItemModule: ["game-ui/editor/widgets/item/editor-item.module.scss", "classes"],
    DirectoryPickerButton: ["game-ui/editor/widgets/fields/directory-picker-button.tsx", "DirectoryPickerButton"],
    StringInputField: ["game-ui/editor/widgets/fields/string-input-field.tsx", "StringInputField"],
    ToggleField: ["game-ui/editor/widgets/fields/toggle-field.tsx", "ToggleField"],
    FloatInputField: ["game-ui/editor/widgets/fields/float-input-field.tsx", "FloatInputField"],
    Float2InputField: ["game-ui/editor/widgets/fields/float-input-field.tsx", "Float2InputField"],
    Float3InputField: ["game-ui/editor/widgets/fields/float-input-field.tsx", "Float3InputField"],
    IntInputField: ["game-ui/editor/widgets/fields/int-input-field.tsx", "IntInputField"],
    HierarchyMenu: ["game-ui/editor/widgets/hierarchy-menu/hierarchy-menu.tsx", "HierarchyMenu"],
    EditorScrollable: ["game-ui/editor/widgets/scrollable/scrollable.tsx", "EditorScrollable"],
    ItemPicker: ["game-ui/editor/widgets/item-picker/item-picker.tsx", "ItemPicker"],
    Checkbox: ["game-ui/common/input/toggle/checkbox/checkbox.tsx", "Checkbox"],
    IntInputStandalone: ["game-ui/common/input/text/int-input.tsx", "IntInput"],
    FloatInputStandalone: ["game-ui/common/input/text/float-input.tsx", "FloatInput"],
}



export class VanillaWidgets {
    public static get instance(): VanillaWidgets { return this._instance ??= new VanillaWidgets() }
    private static _instance?: VanillaWidgets



    private cachedData: Partial<Record<keyof typeof registryIndex, any>> = {}
    private updateCache(entry: keyof typeof registryIndex) {
        const entryData = registryIndex[entry];
        return this.cachedData[entry] = getModule(entryData[0], entryData[1])
    }



    public get themeDropdown(): Theme | any { return this.cachedData["themeDropdown"] ?? this.updateCache("themeDropdown") }
    public get inputField(): Theme | any { return this.cachedData["inputField"] ?? this.updateCache("inputField") }

    public get ColorPicker(): (props: PropsColorPicker) => JSX.Element { return this.cachedData["ColorPicker"] ?? this.updateCache("ColorPicker") }
    public get IntSlider(): (props: PropsIntSlider) => JSX.Element { return this.cachedData["IntSlider"] ?? this.updateCache("IntSlider") }
    public get FloatSlider(): (props: PropsFloatSlider) => JSX.Element { return this.cachedData["FloatSlider"] ?? this.updateCache("FloatSlider") }
    public DropdownField<T>(): (props: PropsDropdownField<T>) => JSX.Element { return this.cachedData["DropdownField"] ?? this.updateCache("DropdownField") }
    public get FocusableEditorItem(): (props: PropsFocusableEditorItem) => JSX.Element { return this.cachedData["FocusableEditorItem"] ?? this.updateCache("FocusableEditorItem") }
    public get editorItemModule(): Theme | any { return this.cachedData["editorItemModule"] ?? this.updateCache("editorItemModule") }
    public EditorItemRow = ({ label, children, styleContent, className }: PropsEditorItemControl) => <VanillaWidgets.instance.FocusableEditorItem focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}>

        <div className={[this.editorItemModule.row, className].join(" ")} style={label ? {} : styleContent}>
            {label ? <><div className={this.editorItemModule.label}>{label}</div><div className={this.editorItemModule.control} style={styleContent}>{children}</div></> : children}
        </div>

    </VanillaWidgets.instance.FocusableEditorItem>
    public EditorItemRowNoFocus = ({ label, children, styleContent, className }: PropsEditorItemControl) =>
        <this.EditorItemRow className={className} label={label} styleContent={styleContent}><FocusDisabled>{children}</FocusDisabled></this.EditorItemRow>
    public get DirectoryPickerButton(): (props: PropsDirectoryPickerButton) => JSX.Element { return this.cachedData["DirectoryPickerButton"] ?? this.updateCache("DirectoryPickerButton") }
    public get StringInputField(): (props: PropsStringInputField) => JSX.Element { return this.cachedData["StringInputField"] ?? this.updateCache("StringInputField") }
    public get ToggleField(): (props: PropsToggleField) => JSX.Element { return this.cachedData["ToggleField"] ?? this.updateCache("ToggleField") }
    public get FloatInputField(): (props: PropsFloatInputField) => JSX.Element { return this.cachedData["FloatInputField"] ?? this.updateCache("FloatInputField") }
    public get Float2InputField(): (props: PropsFloat2InputField) => JSX.Element { return this.cachedData["Float2InputField"] ?? this.updateCache("Float2InputField") }
    public get Float3InputField(): (props: PropsFloat3InputField) => JSX.Element { return this.cachedData["Float3InputField"] ?? this.updateCache("Float3InputField") }
    public get IntInputField(): (props: PropsFloatInputField) => JSX.Element { return this.cachedData["IntInputField"] ?? this.updateCache("IntInputField") }
    public get HierarchyMenu(): (props: PropsHierarchyMenu) => JSX.Element { return this.cachedData["HierarchyMenu"] ?? this.updateCache("HierarchyMenu") }
    public get EditorScrollable(): (props: PropsEditorScrollable) => JSX.Element { return this.cachedData["EditorScrollable"] ?? this.updateCache("EditorScrollable") }

    public get ItemPicker(): (props: ItemPickerProps) => JSX.Element { return this.cachedData["ItemPicker"] ?? this.updateCache("ItemPicker") }
    public get Checkbox(): (props: CheckboxProps) => JSX.Element { return this.cachedData["Checkbox"] ?? this.updateCache("Checkbox") }
    public get IntInputStandalone(): (props: IntInputStandaloneProps) => JSX.Element { return this.cachedData["IntInputStandalone"] ?? this.updateCache("IntInputStandalone") }
    public get FloatInputStandalone(): (props: FloatInputStandaloneProps) => JSX.Element { return this.cachedData["FloatInputStandalone"] ?? this.updateCache("FloatInputStandalone") }

    public get StringInputRow() {
        return ({ label, value, disabled, onChange, className, maxLength, styleContent }: Omit<PropsStringInputField & PropsEditorItemControl, "children" | "multiline" | "onChangeStart" | "onChangeEnd">) => {
            const [typingValue, setTypingValue] = useState(value);
            return <this.EditorItemRow label={label} className={className} styleContent={styleContent}>
                <this.StringInputField value={typingValue} disabled={disabled} onChange={setTypingValue} onChangeStart={() => setTypingValue(value)} onChangeEnd={() => {
                    onChange(typingValue);
                }} maxLength={maxLength} />
            </this.EditorItemRow>
        }
    }
}
