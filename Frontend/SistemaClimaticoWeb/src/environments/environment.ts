export const environment = {
  production: true,
  // Misma direccion que la pagina: el servidor web (Caddy) reenvia /api y /hubs a la API.
  // Asi funciona igual en localhost, en una IP o en un dominio, sin recompilar.
  apiUrl: '/api',
};
