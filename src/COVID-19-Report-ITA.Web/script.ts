import * as resizerObsrv from "./resizer.js";

const target = document.querySelector("#pbi-report") as Element

if (window.ResizeObserver) {
    resizerObsrv.resize(target, document.querySelector("body") as Element);
}
