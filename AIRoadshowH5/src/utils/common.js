/**
 * 将秒数换成时分秒格式 00:00:00
 */
export const formatSeconds = (val) => {
	let result = parseInt(val)
	const h =
		Math.floor(result / 3600) < 10
			? '0' + Math.floor(result / 3600)
			: Math.floor(result / 3600)
	const m =
		Math.floor((result / 60) % 60) < 10
			? '0' + Math.floor((result / 60) % 60)
			: Math.floor((result / 60) % 60)
	const s =
		Math.floor(result % 60) < 10
			? '0' + Math.floor(result % 60)
			: Math.floor(result % 60)
	result = `${m}:${s}`
	return result
}
