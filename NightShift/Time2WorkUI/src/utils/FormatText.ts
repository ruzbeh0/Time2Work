export function formatWords(text: string, forceUpper: boolean = false): string {
    text = text.replace(/([a-z])([A-Z])/g, '$1 $2');
    text = text.replace(/([a-zA-Z])(\d)/g, '$1 $2');
    text = text.replace(/(\d)([a-zA-Z])/g, '$1 $2');
    if (forceUpper) {
        // Capitalize first letter and letters after spaces
        text = text.replace(/(^[a-z])|(\ [a-z])/g, match => match.toUpperCase());
    }
    return text;
}