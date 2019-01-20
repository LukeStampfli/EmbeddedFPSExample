module.exports = {
    base: '/EmbeddedFPSExample/',
    title: 'EmbeddedFPSExample',
    dest: '../docs',
    themeConfig: {
        sidebarDepth: 1,
        sidebar: {
            '/guide/': [
                'introduction',
                'setting-up-the-projects',
                'client-basics-and-login-system-part-1',
                'server-basics-and-login-system',
                'room-system-part-1-server-side',
                'client-login-system-part-2-and-lobby',
                'room-system-part-2-server-side',
                'player-movement-interpolation-and-client-side-prediction',
                'multiplayer-gameplay',
                'reconciliation',
                'health-shooting-lag-Compensation',
				'networking-discussion',
            ]
        }
    }
};