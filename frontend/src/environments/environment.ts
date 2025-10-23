export const environment = {
    production: false,
    apiUrl: 'http://localhost/api',
    signalRUrl: 'http://localhost/hub',
    appVersion: '1.0.0',
    enablePWA: true,
    cacheTimeout: 30 * 24 * 60 * 60 * 1000, // 30 dias em ms - requisito ANAC
    features: {
        offlineMode: true,
        realTimeNotifications: true,
        darkMode: true,
        analytics: false
    },
    anac: {
        maxRetentionDays: 30,
        signatureDeadlines: {
            rbac121: 2, // dias
            rbac135: 15, // dias
            other: 30 // dias
        }
    }
};