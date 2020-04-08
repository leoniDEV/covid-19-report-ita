declare interface ResizeObserverOptions {
    box?: "content-box" | "border-box";
}

declare interface ResizeObserverEntryBoxSize {
    blockSize: number
    inlineSize: number
}

declare interface ResizeObserverEntry {
    readonly borderBoxSize: ResizeObserverEntryBoxSize
    readonly contentBoxSize: ResizeObserverEntryBoxSize
    readonly contentRect: DOMRectReadOnly
    readonly target: Element

}

interface ResizeObserverCallback {
    (entries: ResizeObserverEntry[], observer: ResizeObserver): void
}

declare class ResizeObserver {
    constructor(callback: ResizeObserverCallback)
    disconnect(): void;
    observe(target: Element, options?: ResizeObserverOptions): void;
    unbserve(target: Element): void;
}

interface Window {
    ResizeObserver: ResizeObserver
}