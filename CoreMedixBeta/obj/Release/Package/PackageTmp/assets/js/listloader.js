function getloader(items)
{
    var loader = `<div class="timeline-wrapper">
        <div class="timeline-item">
            <div class="animated-background">
                <div class="background-masker header-top"></div>
                <div class="background-masker header-left"></div>
                <div class="background-masker header-right"></div>
                <div class="background-masker header-bottom"></div>
                <div class="background-masker subheader-left"></div>
                <div class="background-masker subheader-right"></div>
                <div class="background-masker subheader-bottom"></div>
                <div class="background-masker content-top"></div>
                <div class="background-masker content-first-end"></div>
                <div class="background-masker content-second-line"></div>
                <div class="background-masker content-second-end"></div>
                <div class="background-masker content-third-line"></div>
                <div class="background-masker content-third-end"></div>
            </div>
        </div>
    </div>
`;
    var loopreturn="";
    if (items == 0)
    {

        return loader;
    }
    else {
        
        for (i = 1; i <= items; i++) {
            loopreturn += loader;
        }
    }
    return loopreturn;
}

   