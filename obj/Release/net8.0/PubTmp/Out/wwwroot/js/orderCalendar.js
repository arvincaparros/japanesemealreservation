﻿document.addEventListener('DOMContentLoaded', function () {
    const calendarEl = document.getElementById('calendar');
    const calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        events: '../Order/GetOrdersForCalendar',
        eventDisplay: 'block', // for better display
        eventColor: '#3b82f6',  // optional color

        dayCellDidMount: function (info) {
            const today = new Date();
            const cellDate = info.date;

            if (
                cellDate.getFullYear() === today.getFullYear() &&
                cellDate.getMonth() === today.getMonth() &&
                cellDate.getDate() === today.getDate()
            ) {
                info.el.style.backgroundColor = '#dbeafe'; // light blue
                info.el.style.border = '2px solid #3b82f6'; // optional border
            }
        }
    });

    calendar.render();
});