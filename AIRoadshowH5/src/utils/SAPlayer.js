let audioContext = null;
export function CreatePlayer({ ctxCreator, volume = -1, onEnded = null, onError = null }) {
	const silenceAudioId = '_silence_';
	const pageStart = new Date().valueOf();

	function printLog() {
		console.log(`sa:[${new Date().valueOf() - pageStart}] ${Array.prototype.slice.apply(arguments).join(' ')}`);
	}

	/**
	 * 将PCM数据转换为AudioBuffer
	 * @param {*} pcmData 
	 * @param {*} sampleRate 
	 * @param {*} channels 
	 * @param {*} bitDepth 
	 * @returns 
	 */
	function convertPCMToAudioBuffer(pcmData, sampleRate, channels, bitDepth) {
		const bytesPerSample = bitDepth / 8;
		const dataView = new DataView(pcmData instanceof ArrayBuffer ? pcmData : pcmData.buffer);
		const totalFrames = dataView.byteLength / bytesPerSample / channels;

		const audioBuffer = [];

		for (let channel = 0; channel < channels; channel++) {
			const channelData = new Float32Array(totalFrames);
			audioBuffer.push(channelData);
			for (let i = 0; i < totalFrames; i++) {
				const index = (i * channels + channel) * bytesPerSample;

				let sample;
				if (bitDepth === 16) {
					sample = dataView.getInt16(index, true); // 小端
					channelData[i] = sample / 0x8000;
				} else if (bitDepth === 8) {
					sample = dataView.getUint8(index);
					channelData[i] = (sample - 128) / 128;
				} else {
					throw new Error("Unsupported bit depth: " + bitDepth);
				}
			}
		}

		return audioBuffer;
	}

	/**
	 * 去除ID3v2标签
	 * @param {*} arrayBuffer Uint8Array类型的音频数据
	 * @returns 去除ID3v2标签后的Uint8Array
	 */
	function stripID3v2Tag(arrayBuffer) {
		// 检查前3字节是否为'ID3'，即0x49,0x44,0x33
		if (
			arrayBuffer[0] === 0x49 && // 'I'
			arrayBuffer[1] === 0x44 && // 'D'
			arrayBuffer[2] === 0x33    // '3'
		) {
			// ID3v2 Header总长度=10字节头+标签长度（第6-9字节为syncsafe int）
			const size = (
				(arrayBuffer[6] & 0x7f) << 21 |
				(arrayBuffer[7] & 0x7f) << 14 |
				(arrayBuffer[8] & 0x7f) << 7 |
				(arrayBuffer[9] & 0x7f)
			);
			// 跳过ID3v2头部和标签内容
			return arrayBuffer.slice(10 + size);
		}
		return arrayBuffer;
	}

	/**
	 * 去除ID3v1标签
	 * @param {*} arrayBuffer Uint8Array类型的音频数据
	 * @returns 去除ID3v1标签后的Uint8Array
	 */
	function stripID3v1Tag(arrayBuffer) {
		const len = arrayBuffer.length;
		// 检查最后128字节是否以'TAG'开头
		if (
			len >= 128 &&
			arrayBuffer[len - 128] === 0x54 && // 'T'
			arrayBuffer[len - 127] === 0x41 && // 'A'
			arrayBuffer[len - 126] === 0x47    // 'G'
		) {
			// 去除最后128字节
			return arrayBuffer.slice(0, len - 128);
		}
		return arrayBuffer;
	}

	/**
	 * 去除音频数据中的ID3v1和ID3v2标签
	 * @param {*} arrayBuffer ArrayBuffer或Uint8Array
	 * @returns 去除标签后的Uint8Array
	 */
	function stripID3Tag(arrayBuffer) {
		let bytes = new Uint8Array(arrayBuffer);
		bytes = stripID3v1Tag(bytes); // 先去除ID3v1
		bytes = stripID3v2Tag(bytes); // 再去除ID3v2
		return bytes;
	}

	if (!audioContext) {audioContext = ctxCreator();}
	const effectPlayers = [];
	function _createPlayer({ volume = -1, onEnded = null }, isMainPlayer) {
		const gainNode = audioContext.createGain();
		gainNode.connect(audioContext.destination);

		// 静音数据
		const muteData = new Float32Array(8192);
		/**
		 * 播放状态
		 * 0:未开始/已结束;1：播放中;2:已暂停
		 */
		let playState = 0;
		/**
		 * 播放器是否挂起
		 */
		let isSuspended = false;
		// 音频列表
		let audioStreams = {};
		// 当前音频
		let curAudioStream = null;
		// 音量
		let audioVolume = (volume >= 0 && volume <= 2) ? volume : 1;
		// 循环模式
		let loopId = '';
		let isFirstAudio = true;

		/**
		 * 获取音频流
		 * @param {*} id 
		 * @returns 
		 */
		function getAudioStream(audioId, createIfNotExists) {
			audioId = audioId || "";

			// 优先取当前音频
			if (curAudioStream && curAudioStream.id == audioId) {return curAudioStream;}

			let newItem = audioStreams[audioId];
			if (!newItem && !audioId) {newItem = curAudioStream;}

			if (!newItem && (createIfNotExists || audioId == silenceAudioId)) {
				newItem = createAudioStream(audioId);
				audioStreams[audioId] = newItem;
			}

			return newItem;
		}

		/**
		 * 移除音频流
		 * @param {*} id 
		 * @returns 
		 */
		function rmAudioStream(audioId) {
			audioId = audioId || "";

			const s = audioStreams[audioId];
			if (s) {
				if (s == curAudioStream) {
					s.stop();
					curAudioStream = null;
				}
				delete audioStreams[audioId];
			}
		}

		function checkState() {
			if (audioContext && playState == 1) {
				try {
					if (audioContext.state == 'suspended' && !isSuspended) {
						audioContext.resume();
						if (audioContext.state == 'suspended') {printLog(`resume failed`)}
					}
				} catch {
					printLog(`checkState error`)
				}
			}
		}

		/**
		 * 创建音频播放队列
		 */
		function createAudioQueue(channels) {
			const audioQueue = [];
			let totalLength = 0;
			let chunkIndex = 0;
			let dataIndex = 0;
			const totalChannels = channels;
			return {
				/**
				 * 添加数据到队列
				 * @param {number} type 数据类型
				 * @param {function} data 数据
				 * @param {number} startIndex 数据起始下标
				 * @param {number} length 数据长度
				 */
				add(type, data, startIndex, length) {
					// printLog('addAudioData', type, data[0].length, startIndex, length)
					audioQueue.push({
						type: type,
						data: data,
						startIndex: startIndex,
						currentIndex: startIndex,
						endIndex: startIndex + length
					});
					totalLength += length;
				},
				/**
				 * 获取数据
				 * @param {number} length 数据长度
				 * @returns {Uint8Array} 数据
				 */
				get(length) {
					let maxLength = totalLength - dataIndex;
					if (length > maxLength) {length = maxLength;}
					
					if (length <= 0) {return null;}

					const data = audioQueue[chunkIndex];
					maxLength = data.endIndex - data.currentIndex;
					if (length > maxLength) {length = maxLength;}

					const res = [];
					for (let i = 0; i < totalChannels; i++) {
						if (data.type == 0) {
							// 已经解码的音频数据
							res.push(data.data[i].slice(data.currentIndex, data.currentIndex + length));
						} else {
							// 静音数据
							res.push(data.data.slice(data.currentIndex, data.currentIndex + length));
						}
					}
					data.currentIndex += length;
					dataIndex += length;
					if (data.currentIndex >= data.endIndex) {chunkIndex++;}

					return res;
				},
				/**
				 * 获取队列数据长度
				 * @returns {number} 队列数据长度
				 */
				getLength() {
					return totalLength - dataIndex;
				},
				/**
				 * 重置队列
				 */
				reset() {
					dataIndex = 0;
					chunkIndex = 0;
					audioQueue.forEach(item => {
						item.currentIndex = item.startIndex;
					});
				}
			}
		}

		/**
		 * 创建音频流
		 * @param {*} audioId 
		 * @returns 
		 */
		function createAudioStream(audioId) {
			// 是否正在解码数据
			let isProcessing = false;
			// 待解码数据
			const chunkQueue = [];
			// 已解码数据长度
			let decodedDataSamples = 0;
			// 已解码数据采样率
			let decodedDataSampleRate = 0;
			// 已解码数据通道数
			let decodedDataChannels = 0;
			// 待播放数据队列，包括静音片断
			let audioDataQueue = null;
			// 已缓冲的音频数据长度
			let bufferedAudioDataLength = 0;
			// 音频源
			let audioSource = null;
			// 音频缓冲区
			let audioBuffer = null;
			// 处理音频数据定时器
			let timerProcessAudio = 0;
			let timerCheckPlay = 0;
			// 时间轴偏移
			let whenTime = 0;
			// 音频流推送是否结束
			let isChunkEnd = false;
			// 音频流推送次数
			let chunkCount = 0;
			/**
			 * 目标播放状态
			 * 0:未开始/已结束;1：播放中;2:已暂停
			 */
			let reqPlayState = 0;
			/**
			 * 音频类型默认为mp3
			 */
			let audioFmt = null;

			//生成静音数据
			if (audioId == silenceAudioId) {
				isChunkEnd = true;
				audioDataQueue = createAudioQueue(1);
				audioDataQueue.add(1, muteData, 0, muteData.length);
			}

			let decoderWorker = null;

			const ensureDecoderWorker = () => {
				if (!decoderWorker && typeof window !== 'undefined' && window['mpg123-decoder']) {
					decoderWorker = new window['mpg123-decoder'].MPEGDecoderWebWorker();
				}
				return decoderWorker;
			};

			return {
				id: audioId,
				append(chunk) {
					if (isChunkEnd) {
						printLog(`error:音频已经结束！`)
						return;
					}
					if (chunk) {reqPlayState++;}

					if (chunk) {
						printLog('chunk:', chunk.byteLength)
						if (chunk.byteLength) {
							if (!audioFmt || audioFmt.format == 'mp3') {
								chunk = stripID3Tag(chunk);
							} else {
								chunk = new Uint8Array(chunk);
							}
							chunkQueue.push(chunk);
							chunkCount++;
						}
					} else {
						isChunkEnd = true;
						if (chunkCount <= 0) {this.onPlayEnd(true);}
					}
					this.decode();
				},
				setAudioFmt(adFmt) {
					if (adFmt && adFmt.format && !audioFmt) {
						audioFmt = adFmt;
					}
				},
				async decode() {
					printLog(`decode:${isProcessing},q:${chunkQueue.length}`)
					if (!audioContext || isProcessing || chunkQueue.length <= 0) {return;}

					isProcessing = true;
					let oData = null;

					const needsDecoder = !audioFmt || audioFmt.format == 'mp3';
					if (needsDecoder) {
						const worker = ensureDecoderWorker();
						if (worker) {
							await worker.ready;
						}
					}

					while (chunkQueue.length > 0) {
						try {
							oData = chunkQueue.shift();
							let date = null;
							if (needsDecoder) {
								const worker = ensureDecoderWorker();
								if (!worker) { break; }
								const { channelData, samplesDecoded, sampleRate } = await worker.decode(oData);
								date = channelData;
								decodedDataChannels = channelData.length;
								decodedDataSamples += samplesDecoded;
								decodedDataSampleRate = sampleRate;
							} else if (audioFmt.format == 'pcm') {
								date = convertPCMToAudioBuffer(oData, audioFmt.sampleRate, audioFmt.channels, audioFmt.encoding);
								decodedDataChannels = audioFmt.channels;
								decodedDataSamples += date[0].length;
								decodedDataSampleRate = audioFmt.sampleRate;
							} else {
								throw new Error("Unsupported format: " + audioFmt.format);
							}

							if (!audioDataQueue) {
								audioDataQueue = createAudioQueue(decodedDataChannels);
							}
							audioDataQueue.add(0, date, 0, date[0].length);

							if (curAudioStream === this) {this.playAudio();}
						} catch (err) {
							if (onError) {
								onError({
									type: 'decode',
									data: this.id,
									error: err
								});
								break;
							}
							printLog(`decode error: ${err}`);
						} finally {
							if (oData) {reqPlayState -= 1;}
						}
					}
					isProcessing = false;
					clearTimeout(timerProcessAudio);
					const _this = this;
					timerProcessAudio = setTimeout(() => _this.decode(), 20);
					printLog(`decode:${decodedDataSamples}`)
					return true;
				},
				// 拷贝数据到缓冲区
				transferChunk(transferCount, transferStart) {
					const audioData = audioDataQueue.get(transferCount);
					if (!audioData) {return false;}
					// printLog(`transfer-2:${transferCount}|${audioData[0].length}->${transferStart}`)
					// 拷贝数据
					for (let channel = 0; channel < audioData.length; channel++) {
						audioBuffer.copyToChannel(audioData[channel], channel, transferStart);
					}
					bufferedAudioDataLength += audioData[0].length;
					return audioData[0].length == transferCount;

				},
				/**
				 * 拷贝数据到缓冲区
				 * @param {boolean} decodeEnded 是否解码结束
				 */
				transfer(decodeEnded) {
					const playedAudioDataLength = parseInt((audioContext.currentTime - whenTime) * decodedDataSampleRate);
					// 预加载数据长度
					const preload = bufferedAudioDataLength - playedAudioDataLength;
					// 如果预加载数据不足，则添加静音数据
					if (audioDataQueue.getLength() <= 0 && preload < decodedDataSampleRate * 0.05 && !decodeEnded) {
						audioDataQueue.add(1, muteData, 0, Math.min((decodedDataSampleRate * 0.05 - preload), muteData.length));
					}

					// 如果预加载数据不足，则拷贝数据
					if (preload < audioBuffer.length - decodedDataSampleRate * 4) {
						const transferStart0 = bufferedAudioDataLength % audioBuffer.length;
						let transferCount0 = 0;
						let transferCount1 = 0;
						// 播放缓冲区真实地址
						const playBufferIndex1 = playedAudioDataLength % audioBuffer.length;
						if (transferStart0 >= playBufferIndex1) {
							transferCount0 = audioBuffer.length - transferStart0;
							transferCount1 = playBufferIndex1 - (decodedDataSampleRate * 2);
						} else {
							transferCount0 = playBufferIndex1 - transferStart0;
						}

						// 拷贝数据
						if (this.transferChunk(transferCount0, transferStart0)) {this.transferChunk(transferCount1, 0);}
					}
				},
				playAudio() {
					printLog(`playAudio:${this.id},${playState},${decodedDataSamples}`);
					if (decodedDataSamples <= 0 || audioSource || !audioDataQueue) {return;}

					printLog(`playAudio.1`);
					checkState();
					// 创建
					audioSource = audioContext.createBufferSource();
					whenTime = audioContext.currentTime > 0 ? audioContext.currentTime + 0.01 : 0;
					// 分片/流式
					// buffer长度为8秒时长
					audioBuffer = audioContext.createBuffer(decodedDataChannels, decodedDataSampleRate * 8, decodedDataSampleRate);
					// copy&检查音频数据,audioContext循环使用，currentTime会累计，需要做偏移
					this.checkPlay();
					audioSource.buffer = audioBuffer;
					// 循环播放
					audioSource.loop = true;
					audioSource.connect(gainNode);
					// 设置音量
					if (this.id != silenceAudioId) {gainNode.gain.value = audioVolume;}
					audioSource.start(whenTime);
					playState = 1;
					checkState();
				},
				checkPlay() {
					const totalTime = (bufferedAudioDataLength + audioDataQueue.getLength()) / decodedDataSampleRate;
					const decodeEnded = isChunkEnd && !isProcessing && chunkQueue.length == 0;
					if (decodeEnded && audioContext.currentTime - whenTime > totalTime) {
						//播放结束
						// printLog(`checkPlay:doStop:${audioContext.currentTime.toFixed(2)}|${whenTime.toFixed(2)}|${totalTime.toFixed(2)}`)
						this.doStop(false);
						return;
					} else {
						if (isMainPlayer) {checkState();}
						this.transfer(decodeEnded);
					}

					clearTimeout(timerCheckPlay);
					const _this = this;
					timerCheckPlay = setTimeout(() => _this.checkPlay(), 20);
				},
				/**
				 * 停止
				 */
				stop() {
					this.doStop(true);
				},
				/**
				 * 停止
				 */
				doStop(forceNotify) {
					printLog(`doStop:${this.id}`)
					if (audioSource) {
						try {
							audioSource.stop();
						} catch (e) {
							printLog(`doStop.err1:${e.message}`);
						}
						try {
							audioSource.disconnect(gainNode);
						} catch (e) {
							printLog(`doStop.err1:${e.message}`);
						}
						audioSource = null;
						clearTimeout(timerCheckPlay);
						clearTimeout(timerProcessAudio);

						if (forceNotify) {isChunkEnd = true;}
						this.onPlayEnd(true);

						isProcessing = false;
						audioDataQueue.reset();
						bufferedAudioDataLength = 0;
						audioBuffer = null;
						timerProcessAudio = 0;
						timerCheckPlay = 0;
						isSuspended = false;
						playState = 0;
						chunkCount = 0;
					}
					if (loopId == this.id) {
						this.playAudio();
					} else if ((curAudioStream || {}).id == this.id) {
						curAudioStream = null;
					}
					if (decoderWorker && isChunkEnd) {
						try {
							decoderWorker.free();
						} catch (error) {
							printLog(`Error freeing decoderWorker: ${error}`);
						}
						decoderWorker = null;
					}
				},
				onPlayEnd(isStoped) {
					printLog(`onPlayEnd:${isChunkEnd},${reqPlayState},${audioSource}`)
					if ((isStoped || (isChunkEnd && reqPlayState <= 0)) && this.id != silenceAudioId && !audioSource && typeof onEnded === 'function') {
						setTimeout(() => onEnded(this.id), 4);//防止线程阻塞
					}
				},
				isChunkEnd(checkDecode) {
					printLog(`isChunkEnd:${checkDecode},${reqPlayState},${checkDecode}`)
					return isChunkEnd && (!checkDecode || reqPlayState <= 0);
				},
				/**
				* 唤醒
				* @param {*} suspendedTime 挂起时间
				*/
				onResume(suspendedTime) {
					printLog(`onResume:${suspendedTime}`);
					if (audioSource) {
						whenTime += suspendedTime;
					}
				}
			};
		}

		function switchTo(nextAudio) {
			if (curAudioStream && curAudioStream.id != nextAudio.id) {
				curAudioStream.stop();
			}
			curAudioStream = nextAudio;
		}

		function reset() {
			if (isSuspended) {
				audioContext.resume();
				isSuspended = false;
			}
			playState = 0;
			audioStreams = {};
		}

		const player = {
			/**
			 * 播放
			 */
			play(audioId = '', loop = 0, volume = -1) {
				// 静音片断只能在最开始播放
				if (!isFirstAudio && audioId == silenceAudioId) {return;}
				isFirstAudio = false;

				loopId = loop ? audioId : '';
				if (volume >= 0 && volume <= 2) {audioVolume = volume;}
				printLog(`play:${audioId}:${playState}`)
				const audioStream = getAudioStream(audioId);
				if (audioStream) {switchTo(audioStream);}

				if (playState == 0) {
					if (curAudioStream) {curAudioStream.playAudio();}
				} else if (playState == 2) {
					playState = 1;
					if (!isSuspended) {audioContext.resume()}
				}
			},
			/**
			 * 添加音频片断到播放队列尾部
			 * @param {*} chunk 音频片断，为空则表示已无后续片断
			 * @param {*} autoPlay 是否自动播放
			 * @param {*} audioId 音频Id
			 */
			append(chunk, autoPlay, audioId = '', audioFmt = null) {
				printLog(`append:${chunk},${autoPlay},${audioId}`)
				let audioStream = getAudioStream(audioId, false);
				const needPlay = autoPlay && (!audioStream || !curAudioStream || audioStream === curAudioStream);

				if (!audioStream) {
					audioStream = getAudioStream(audioId, true);
					if (audioFmt) {audioStream.setAudioFmt(audioFmt);}
				}

				if (needPlay) {this.play(audioId);}

				audioStream.append(chunk, needPlay);
			},
			/**
			 * 停止
			 */
			stop() {
				printLog('stop:sub');
				loopId = '';
				curAudioStream && curAudioStream.stop(true);
			},
			/**
			 * 当前是否正在播放
			 */
			isPlay() {
				printLog(`isPlay:sub:${isSuspended},${curAudioStream},${playState}`)
				return !isSuspended && !!curAudioStream && playState == 1 //|| !curAudioStream.isChunkEnd(true); //audioSource存在，则代表正在播放
			},
			/**
			 * 当前是否已经播放完
			 */
			isPlayEnd() {
				printLog(`isPlayEnd:sub:${playState}`)
				return /*(!curAudioStream || curAudioStream.isChunkEnd(true)) && */playState == 0; //audioSource存在，则代表正在播放
			},
			/**
			 * 检测是否存在某段音频
			 * @param {*} audioId 音频Id
			 * @param {*} clearIfNotFinished 如果数据不完整则移除
			 */
			hasAudio(audioId, clearIfNotFinished) {
				const audioStream = getAudioStream(audioId, false);
				if (audioStream) {
					printLog('hasAudio', audioId, audioStream, !clearIfNotFinished)
					if (audioStream.isChunkEnd(false) || !clearIfNotFinished) {return true;}

					rmAudioStream(audioId);
				}

				return false;
			},
			/**
			 * 移除某段音频
			 * @param {*} audioId 音频Id
			 */
			rmAudio(audioId) {
				rmAudioStream(audioId);
			},
			/**
			 * 唤醒
			 * @param {*} suspendedTime 挂起时间
			 */
			onResume(suspendedTime) {
				printLog('onResume');
				if (curAudioStream) {curAudioStream.onResume(suspendedTime);}
			}
		};

		return isMainPlayer ? {
			__proto__: player,
			checkState,
			/**
			 * 暂停
			 */
			pause() {
				printLog(`pause:${playState}`);
				if (playState == 1) {
					audioContext.suspend();
					playState = 2;
				}
			},
			/**
			 * 挂起
			 */
			suspend() {
				printLog('suspend');
				if (audioContext && curAudioStream) {
					if (playState != 2) {audioContext.suspend();}
					isSuspended = true;
				}
			},
			/**
			 * 唤醒
			 */
			resume() {
				printLog('resume');
				if (isSuspended && audioContext && curAudioStream) {
					if (playState != 2) {audioContext.resume();}
					isSuspended = false;
				}
			},
			/**
			 * 停止
			 */
			stop() {
				printLog('stop:main');
				effectPlayers.forEach(p => p.stop());
				player.stop();
			},
			/**
			 * 销毁
			 */
			close(destoryAudioContext) {
				printLog('close');
				if (audioContext) {
					this.stop();
					if (destoryAudioContext) {
						try {
							audioContext.close();
						} catch (error) {
							printLog(`Error during close operation: ${error}`);
						}
						audioContext = null;
					}
					reset();
				}
			},
			/**
			 * 当前是否正在播放
			 */
			isPlay() {
				printLog(`isPlay:main`)
				let res = false;
				effectPlayers.forEach(p => res = res || p.isPlay());
				return res || player.isPlay();
			},
			/**
			 * 当前是否已经播放完
			 */
			isPlayEnd() {
				printLog(`isPlayEnd:main`)
				let res = false;
				effectPlayers.forEach(p => res = res || p.isPlayEnd());
				return res || player.isPlayEnd();
			},
			/**
			 * 当前是否正在播放
			 */
			isSuspended() {
				return isSuspended;
			},
			createEffectPlayer({ onEnded = null, volume = -1 }) {
				const p = _createPlayer({ volume, onEnded }, false);
				effectPlayers.push(p);
				return p;
			}
		} : player;
	}

	const player = _createPlayer({ volume, onEnded }, true);
	// 首次用户交互时播放静音音效,用于解锁
	const firstEnevt = () => {
		window.removeEventListener('touchend', firstEnevt, true);
		window.removeEventListener('click', firstEnevt, true);
		player.play(silenceAudioId);
		effectPlayers.forEach(p => p.play(silenceAudioId));
	};
	window.addEventListener('touchend', firstEnevt, true);
	window.addEventListener('click', firstEnevt, true);
	let suspendedStart = 0;
	/**
	 * 音频上下文状态改变
	 */
	audioContext.onstatechange = () => {
		if (audioContext.state === 'running') {
			if (suspendedStart > 0) {
				const suspendedTime = audioContext.currentTime - suspendedStart;
				suspendedStart = 0;
				// printLog(`suspendedTime:${suspendedTime}`);
				player.onResume(suspendedTime);
				effectPlayers.forEach(p => p.onResume(suspendedTime));
			}
		} else if (audioContext.state === 'suspended') {
			suspendedStart = audioContext.currentTime;
		} else if (audioContext.state === 'closed') {
			suspendedStart = 0;
		}
	};
	return player;
}
