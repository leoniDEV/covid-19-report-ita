function resize(target: Element, observedObj: Element): void {
    const resizeObsrv: ResizeObserver = new ResizeObserver((entries: ResizeObserverEntry[]) => {
        entries.forEach(element => {
            (target as HTMLElement).style.width = `${element.contentRect.width * .975}px`;
            (target as HTMLElement).style.height= `${element.contentRect.height * .9}px`;
        });
    })

    resizeObsrv.observe(observedObj, {box: "content-box"})
}

export { resize };