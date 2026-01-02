window.setupNotificationClickOutside = (element, dotNetRef) => {
    const handleClickOutside = (event) => {
        if (element && !element.contains(event.target)) {
            dotNetRef.invokeMethodAsync('CloseDropdown');
        }
    };

    // Add event listener
    document.addEventListener('click', handleClickOutside);

    // Return cleanup function
    return {
        dispose: () => {
            document.removeEventListener('click', handleClickOutside);
        }
    };
};

