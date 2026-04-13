import { Component } from "react";
import { VanillaComponentResolver } from "../VanillaComponentResolver";
import { useState } from "react";

type VectorSectionProps = {
    title: string;
    valueGetter: () => string[];
};

export class VectorSection extends Component<VectorSectionProps> {

    render() {
        return <>
            <VanillaComponentResolver.instance.Section title={this.props.title}>
                {this.props.valueGetter()?.map((x, i) => <div key={i} className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}>{x}</div>)}
            </VanillaComponentResolver.instance.Section>
        </>;

    }
}

type VectorSectionEditableProps = {
    title: string;
    valueGetterFormatted: () => string[];
    valueGetter: () => string[];
    onValueChanged: (i: number, x: string) => any;
    extraContent?: React.ReactNode
};
export function VectorSectionEditable({ title, valueGetterFormatted, valueGetter, onValueChanged, extraContent }: VectorSectionEditableProps) {
    const [editingValue, setEditingValue] = useState<string[]>([]);
    const [editingIdx, setEditingIdx] = useState<number>(-1);

    const formattedVals = valueGetterFormatted();
    const width = `${68 * 3 / (formattedVals?.length || 1)}rem`;

    return (
        <>
            <VanillaComponentResolver.instance.Section title={title}>
                {valueGetter()?.map((x, i) => {
                    const onBlur = (target: any) => {
                        onValueChanged(i, target.value);
                        setEditingValue([]);
                        setEditingIdx(-1);
                    };

                    return (
                        <div
                            key={i}
                            style={{ position: "relative", maxWidth: width, display: 'flex', flexGrow: 1, flexShrink: 1 }}
                            className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}
                        >
                            <div
                                style={{
                                    opacity: editingIdx === i ? 0 : 1,
                                    pointerEvents: "none",
                                    display: "flex",
                                    position: "absolute",
                                    left: 0,
                                    right: 0,
                                    top: 0,
                                    bottom: 0,
                                    width: "100%",
                                }}
                                className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}
                            >
                                {formattedVals[i]}
                            </div>
                            <div
                                style={{
                                    opacity: editingIdx === i ? 1 : 0,
                                    display: "flex",
                                    position: "absolute",
                                    left: 0,
                                    right: 0,
                                    top: 0,
                                    bottom: 0,
                                    width: "100%",
                                }}
                                className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}
                                onClick={() => {
                                    const newEditingValue = [];
                                    newEditingValue[i] = x;
                                    setEditingIdx(i);
                                    setEditingValue(newEditingValue);
                                }}
                            >
                                {editingIdx === i && (
                                    <input
                                        key={i}
                                        className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField}
                                        style={{
                                            display: "flex",
                                            position: "absolute",
                                            left: 0,
                                            right: 0,
                                            top: 0,
                                            bottom: 0,
                                            border: "none",
                                            background: "none",
                                            color: "white",
                                            textAlign: "center",
                                            fontWeight: "normal",
                                            paddingTop: "5rem",
                                            width: "100%",
                                        }}
                                        value={editingValue[i] ?? ""}
                                        onChange={e => {
                                            const newVal = [...editingValue];
                                            newVal[i] = e.target.value;
                                            setEditingValue(newVal);
                                        }}
                                        autoFocus={true}
                                        onFocus={e => {
                                            e.target.selectionStart = e.target.value.length;
                                        }}
                                        onDoubleClick={e => {
                                            e.currentTarget.selectionStart = 0;
                                            e.currentTarget.selectionEnd = e.currentTarget.value.length;
                                        }}
                                        onBlur={e => onBlur(e.target)}
                                        onKeyDownCapture={e => {
                                            if (e.key === "Enter") {
                                                onBlur(e.target);
                                                e.stopPropagation();
                                            } else if (e.key === "Escape") {
                                                setEditingValue([]);
                                                setEditingIdx(-1);
                                                e.stopPropagation();
                                            }
                                        }}
                                    />
                                )}
                            </div>
                        </div>
                    );
                })}
                {extraContent}
            </VanillaComponentResolver.instance.Section>
        </>
    );
}
