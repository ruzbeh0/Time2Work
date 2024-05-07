export type Styles = {
    "week": string;
    "center-box": string;
    content: string;
};

export type ClassNames = keyof Styles;

declare const styles: Styles;

export default styles;
