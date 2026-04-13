import { Component } from "react";
import { VanillaComponentResolver } from "../VanillaComponentResolver";

type AmountValueSectionProps = {
    title?: string | JSX.Element;
    down: {
        tooltip?: string;
        onSelect: () => any;
        disabledFn?: () => boolean;
    };
    up: {
        tooltip?: string;
        onSelect: () => any;
        disabledFn?: () => boolean;
    };
    widthContent?: number,
    valueGetter: () => string;
    sectionless?: boolean;
    disabled?: boolean;
    actions?: {
        icon: string,
        onSelect: () => any,
        tooltip?: string,
        selected?: boolean,
        disabledFn?: () => boolean
    }[]
};
export class AmountValueSection extends Component<AmountValueSectionProps> {

    render() {
        const baseUrl = "assetdb://GameUI/Media/Glyphs/";
        const arrowDownSrc = baseUrl + "ThickStrokeArrowDown.svg";
        const arrowUpSrc = baseUrl + "ThickStrokeArrowUp.svg";
        const content = <><VanillaComponentResolver.instance.ToolButton
            className={VanillaComponentResolver.instance.mouseToolOptionsTheme.startButton}
            tooltip={this.props.down.tooltip}
            onSelect={this.props.down.onSelect}
            src={arrowDownSrc}
            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
            disabled={this.props.disabled || this.props.down.disabledFn?.()}
        />
            <div className={VanillaComponentResolver.instance.mouseToolOptionsTheme.numberField + (this.props.disabled ? " disabled" : "")} style={this.props.widthContent!! > 20 && this.props.widthContent!! < 247 ? { width: this.props.widthContent + "rem" } : this.props.title ? {} : { width: "247rem" }}>{this.props.valueGetter()}</div>
            <VanillaComponentResolver.instance.ToolButton
                className={VanillaComponentResolver.instance.mouseToolOptionsTheme.endButton}
                tooltip={this.props.up.tooltip}
                onSelect={this.props.up.onSelect}
                src={arrowUpSrc}
                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                disabled={this.props.disabled || this.props.up.disabledFn?.()}
            />
            {
                <>
                    {this.props.actions?.map((x, i) =>
                        <VanillaComponentResolver.instance.ToolButton
                            key={i}
                            className={VanillaComponentResolver.instance.toolButtonTheme.button}
                            tooltip={x.tooltip}
                            onSelect={x.onSelect}
                            src={x.icon}
                            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                            disabled={x.disabledFn?.()}
                        ></VanillaComponentResolver.instance.ToolButton>
                    )}
                </>
            }</>;
        if (this.props.sectionless) return content;
        return <>
            <VanillaComponentResolver.instance.Section title={this.props.title}>
                {content}
            </VanillaComponentResolver.instance.Section>
        </>

    }
}


